﻿using System;
using System.Linq;
using CallWall.Contract;
using CallWall.Contract.Contact;
using CallWall.Google.AccountConfiguration;
using CallWall.Google.Authorization;
using CallWall.Google.Authorization.Login;
using CallWall.Google.Providers.Contacts;
using CallWall.Testing;
using Microsoft.Practices.Unity;
using Moq;
using NUnit.Framework;

// ReSharper disable ReplaceWithSingleCallToSingle
// ReSharper disable InconsistentNaming
namespace CallWall.Google.UnitTests
{
    public abstract class Given_a_constructed_GoogleModule
    {
        #region Setup

        private Given_a_constructed_GoogleModule()
        {
        }

        private GoogleModule _hostModule;
        private Mock<ITypeRegistry> _containerMock;
        private Mock<ILoginController> _resolvedLoginController;

        [SetUp]
        public virtual void SetUp()
        {
            _containerMock = new Mock<ITypeRegistry>();
            _resolvedLoginController = new Mock<ILoginController>();
            _hostModule = new GoogleModule(_containerMock.Object, ()=>_resolvedLoginController.Object);

        }

        #endregion

        [TestFixture]
        public sealed class When_Initialised : Given_a_constructed_GoogleModule
        {
           
            public override void SetUp()
            {
                base.SetUp();
                
                _hostModule.Initialize();
            }

            [Test]
            public void Should_register_GoogleAccountConfiguration_instance()
            {
                //_containerMock.Verify(c => c.RegisterType(typeof(IAccountConfiguration), typeof(GoogleAccountConfiguration), "GoogleAccountConfiguration", It.IsAny<ContainerControlledLifetimeManager>()), Times.Once());
                _containerMock.Verify(c => c.RegisterCompositeAsSingleton<IAccountConfiguration,GoogleAccountConfiguration>(), Times.Once());
            }

            [Test]
            public void Should_register_GoogleAuthorization_instance()
            {
                //_containerMock.Verify(c => c.RegisterType(typeof(IGoogleAuthorization), typeof(GoogleAuthorization), (string)null, It.IsAny<ContainerControlledLifetimeManager>()), Times.Once());
                _containerMock.Verify(c => c.RegisterTypeAsSingleton<IGoogleAuthorization,GoogleAuthorization>(), Times.Once());
            }

            [Test]
            public void Should_register_GoogleLoginView_instance()
            {
                //_containerMock.Verify(c => c.RegisterType(typeof(IGoogleLoginView), typeof(GoogleLoginView), (string)null, It.IsAny<ContainerControlledLifetimeManager>()), Times.Once());
                _containerMock.Verify(c => c.RegisterTypeAsSingleton<IGoogleLoginView,GoogleLoginView>(), Times.Once());
            }
            [Test]
            public void Should_register_LoginController_instance()
            {
                //_containerMock.Verify(c => c.RegisterType(typeof(ILoginController), typeof(LoginController), (string)null, It.IsAny<ContainerControlledLifetimeManager>()), Times.Once());
                _containerMock.Verify(c => c.RegisterTypeAsSingleton<ILoginController,LoginController>(), Times.Once());
            }

            [Test]
            public void Should_register_GoogleAccountSetupView_instance()
            {
                //_containerMock.Verify(c => c.RegisterType(typeof(IGoogleAccountSetupView), typeof(GoogleAccountSetupView), (string)null, It.IsAny<ContainerControlledLifetimeManager>()), Times.Once());
                _containerMock.Verify(c => c.RegisterTypeAsSingleton<IGoogleAccountSetupView,GoogleAccountSetupView>(), Times.Once());
            }

            [Test]
            public void Should_register_GoogleAccountSetupViewModel_instance()
            {
                //_containerMock.Verify(c => c.RegisterType(typeof(IGoogleAccountSetupViewModel), typeof(GoogleAccountSetupViewModel), (string)null, It.IsAny<ContainerControlledLifetimeManager>()), Times.Once());
                _containerMock.Verify(c => c.RegisterTypeAsSingleton<IGoogleAccountSetupViewModel,GoogleAccountSetupViewModel>(), Times.Once());
            }
            
            /*
                This is a strategy for registering a singleton but as two separate types. This maintains lazy instantiation.
             
             */
            [Test]
            public void Should_register_GoogleContactQueryProvider_to_container_as_IGoogleContactQueryProvider()
            {
                //var container = new StubUnityContainer();
                //var loginControllerMock = new Mock<ILoginController>();
                //container.RegisterInstance(typeof(ILoginController), loginControllerMock.Object);

                //var sut = new GoogleModule(container);

                //sut.Initialize();
                //container.RegisteredTypes
                //    .Where(rt => rt.From == typeof(IGoogleContactQueryProvider))
                //    .Where(rt => rt.To == typeof(GoogleContactQueryProvider))
                //    .Where(rt => rt.Name == null)
                //    .Where(rt => rt.LifetimeManager is ContainerControlledLifetimeManager)
                //    .Single();

                _containerMock.Verify(c => c.RegisterCompositeAsSingleton<IContactQueryProvider,IGoogleContactQueryProvider, GoogleContactQueryProvider>(), Times.Once());
            }
            //[Test]
            //public void Should_register_GoogleContactQueryProvider_to_container_as_named_IContactQueryProvider()
            //{
            //    var container = new StubUnityContainer();
            //    var loginControllerMock = new Mock<ILoginController>();
            //    container.RegisterInstance(typeof (ILoginController), loginControllerMock.Object);


            //    var sut = new GoogleModule(container);
            //    sut.Initialize();
            //    container.RegisteredTypes
            //        .Where(rt => rt.From == typeof(IContactQueryProvider))
            //        .Where(rt => rt.To == typeof(GoogleContactQueryProvider))
            //        .Where(rt => rt.Name == "GoogleContactQueryProvider")
            //        .Where(rt => rt.LifetimeManager is ExternallyControlledLifetimeManager) //} HACK: A poor way of checking if the instance is registered
            //        .Where(rt => rt.InjectionMembers.Single() is InjectionFactory)          //} to actually redirect to the registered IDemoActivatedIdentityListener instance/type.
            //        .Single();
            //}
            

            [Test]
            public void Should_start_LoginController()
            {
                _resolvedLoginController.Verify(lc=>lc.Start(), Times.Once());
            }
        }
    }
}
// ReSharper restore InconsistentNaming
// ReSharper restore ReplaceWithSingleCallToSingle