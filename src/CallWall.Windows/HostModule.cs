using CallWall.Windows;
using CallWall.Windows.Web;
using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Unity;

namespace CallWall
{
    public sealed class HostModule : IModule
    {
        private readonly IUnityContainer _container;

        public HostModule(IUnityContainer container)
        {
            _container = container;
        }

        public void Initialize()
        {
            _container.RegisterType<ISchedulerProvider, SchedulerProvider>(new ContainerControlledLifetimeManager());

            _container.RegisterType<ILocalStoragePersistence, LocalStoragePersistence>(new ContainerControlledLifetimeManager());
            _container.RegisterType<IJsonSerializer, JsonSerializer>(new ContainerControlledLifetimeManager());
            _container.RegisterType<IPersonalizationSettings, PersonalizationSettings>(new ContainerControlledLifetimeManager());
#if !FAKE
            _container.RegisterType<IHttpClient, HttpClient>(new ContainerControlledLifetimeManager());
#endif
        }
    }
}