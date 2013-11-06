﻿#if FAKE
using CallWall.FakeProvider;
#endif
using System.Linq;
using System.Text;
using CallWall.Contract;
using CallWall.Google;
using CallWall.Logging;
using CallWall.PrismExtensions;
using CallWall.Windows.Connectivity;
using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.UnityExtensions;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Practices.Unity;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace CallWall
{
    public sealed class Bootstrapper : UnityBootstrapper, IDisposable
    {
        private readonly LoggerFactory _loggerFactory;
        private readonly ILogger _logger;

        public Bootstrapper()
        {
            Thread.CurrentThread.Name = "UI";
            _loggerFactory = new LoggerFactory();
            _logger = _loggerFactory.CreateLogger(GetType());
            _logger.Info("-------------------------------------------------------------------------------");
            _logger.Info("Starting application");
        }

        protected override Microsoft.Practices.Prism.Logging.ILoggerFacade CreateLogger()
        {
            return (Microsoft.Practices.Prism.Logging.ILoggerFacade)_logger;
        }

        protected override IModuleCatalog CreateModuleCatalog()
        {
            return new ModuleCatalog();
        }

        protected override void ConfigureModuleCatalog()
        {
            base.ConfigureModuleCatalog();
            ModuleCatalog.Add<HostModule>();
            ModuleCatalog.Add<Shell.ShellModule>();
            ModuleCatalog.Define<Settings.SettingsModule>()
                         .DependsOn<Shell.ShellModule>()
                         .Add();
            ModuleCatalog.Add<BluetoothModule>();

            ModuleCatalog.Define<GoogleModule>()
                         .DependsOn<HostModule>()
                         .Add();
#if FAKE
            ModuleCatalog.Add<FakeModule>();
#endif

            ModuleCatalog.Define<ProfileDashboard.DashboardModule>()
                         .DependsOn<Shell.ShellModule>()
                         .DependsOn<Settings.SettingsModule>()
                         .Add();

            ModuleCatalog.Define<Welcome.WelcomeModule>()
                         .DependsOn<Settings.SettingsModule>()
                         .DependsOn<GoogleModule>()
#if FAKE
                         .DependsOn<FakeModule>()
#endif
                         .Add();

            ModuleCatalog.Define<Toolbar.ToolbarModule>()
                         .DependsOn<Settings.SettingsModule>()
                         .DependsOn<GoogleModule>()
#if FAKE
                         .DependsOn<FakeModule>()
#endif
                         .Add();

        }

        protected override void ConfigureContainer()
        {
            base.ConfigureContainer();
            Container.AddNewExtension<GenericSupportExtension>();
            Container.RegisterInstance<ILoggerFactory>(_loggerFactory);
            Container.RegisterType<ITypeRegistry, TypeRegistry>(new ContainerControlledLifetimeManager());
        }

        protected override Microsoft.Practices.Prism.Regions.RegionAdapterMappings ConfigureRegionAdapterMappings()
        {
            var baseMappings = base.ConfigureRegionAdapterMappings();
            baseMappings.RegisterMapping(typeof(System.Windows.Controls.Accordion), ServiceLocator.Current.GetInstance<AccordionRegionAdapter>());


            var windowStyle = (Style)App.Current.FindResource("WindowRegionStyle");
            RegionPopupBehaviors.RegisterNewWindowRegion(RegionNames.WindowRegion, windowStyle);

            var popupStyle = (Style)App.Current.FindResource("PopupRegionStyle");
            RegionPopupBehaviors.RegisterNewWindowRegion(RegionNames.PopupWindowRegion, popupStyle);

            return baseMappings;
        }

        //protected override void ConfigureServiceLocator()
        //{
        //    base.ConfigureServiceLocator();
        //}

        //protected override Microsoft.Practices.Prism.Regions.IRegionBehaviorFactory ConfigureDefaultRegionBehaviors()
        //{
        //    return base.ConfigureDefaultRegionBehaviors();
        //}

        //protected override void RegisterFrameworkExceptionTypes()
        //{
        //    base.RegisterFrameworkExceptionTypes();
        //}

        //protected override void InitializeShell()
        //{
        //    base.InitializeShell();
        //}

        protected override void InitializeModules()
        {
            _logger.Debug("InitializeModules()");

            var sb = new StringBuilder();
            sb.AppendLine("Modules registered are:");

            var modulesDescriptions = ModuleCatalog.Modules.OrderBy(mi => mi.DependsOn.Count);
            foreach (var modulesDescription in modulesDescriptions)
            {
                sb.AppendFormat("'{0}' depends on '{1}'", modulesDescription.ModuleName,
                                string.Join("', '", modulesDescription.DependsOn));
                sb.AppendLine();
            }
            _logger.Debug(sb.ToString());

            base.InitializeModules();
        }

        protected override DependencyObject CreateShell()
        {
            //Yeah pretty weird huh, this app doesn't have a shell. Technically it is a service that can show visuals (which end up being windows.) However closing a window doesn't indicate the end of the process. -LC
            return null;
        }

        public void LogFailure(string source, DispatcherUnhandledExceptionEventArgs args)
        {
            _logger.Fatal(args.Exception, "An unhandled exception occurred on the Dispatcher. args.Handled={0}", args.Handled);
        }

        public void LogFailure(string source, UnhandledExceptionEventArgs args)
        {
            _logger.Fatal("An unhandled exception occurred on the Dispatcher. args.IsTerminating={0}\r\nargs.ExceptionObject={1}", args.IsTerminating, args.ExceptionObject);
        }

        public void Dispose()
        {
            _logger.Debug("Bootstrapper disposing");
            Container.Dispose();
            _logger.Debug("Container disposed");
            _logger.Info("Application shutting down");
        }
    }
}
