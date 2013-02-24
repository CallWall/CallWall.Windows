﻿using CallWall.Google.AccountConfiguration;
using CallWall.Google.Authorization;
using Microsoft.Reactive.Testing;
using Moq;
using NUnit.Framework;
using System;
using System.Reactive;
using System.Reactive.Linq;

// ReSharper disable InconsistentNaming
namespace CallWall.Google.UnitTests.Authorization
{
    public abstract class Given_a_newly_constructed_GoogleAuthorization
    {
        #region Setup guff

        private GoogleAuthorization _sut;
        private Mock<IPersonalizationSettings> _localStoreMock;
        private Mock<IGoogleOAuthService> _oAuthServiceMock;
        private Mock<ILogger> _loggerMock;

        private Given_a_newly_constructed_GoogleAuthorization()
        {
        }

        [SetUp]
        public virtual void SetUp()
        {
            _localStoreMock = new Mock<IPersonalizationSettings>();
            _oAuthServiceMock = new Mock<IGoogleOAuthService>();
            _loggerMock = new Mock<ILogger>();
            var logFactory = new Mock<ILoggerFactory>();
            logFactory.Setup(lf => lf.CreateLogger(It.IsAny<Type>())).Returns(_loggerMock.Object);
            // logFactory.Setup(lf => lf.CreateLogger(It.IsAny<Type>())).Returns(new DebugLogger());
            _sut = new GoogleAuthorization(_localStoreMock.Object, _oAuthServiceMock.Object, logFactory.Object);
        }

        #endregion

        [TestFixture]
        public sealed class When_current_Session_has_been_persisted : Given_a_newly_constructed_GoogleAuthorization
        {
            //TODO: Should...
        }

        [TestFixture]
        public sealed class When_expired_Session_has_been_persisted : Given_a_newly_constructed_GoogleAuthorization
        {
            //TODO: Should....
        }

        [TestFixture]
        public sealed class When_no_callback_is_registered_for_GoogleAuthorization : Given_a_newly_constructed_GoogleAuthorization
        {
            private readonly GoogleResource[] _requestedResources = new[] { GoogleResource.Contacts };
            [Test]
            public void Should_return_false_for_IsAuthorized_Status()
            {
                Assert.IsFalse(_sut.Status.IsAuthorized);
            }

            [Test]
            public void Should_return_false_for_IsProcessing_Status()
            {
                Assert.IsFalse(_sut.Status.IsProcessing);
            }

            [Test]
            public void Should_fail_when_Authorize_is_called()
            {
                var observer = new TestScheduler().CreateObserver<Unit>();
                _sut.Authorize(_requestedResources).Subscribe(observer);
                Assert.AreEqual(1, observer.Messages.Count);
                Assert.AreEqual(NotificationKind.OnError, observer.Messages[0].Value.Kind);
                Assert.IsInstanceOf<InvalidOperationException>(observer.Messages[0].Value.Exception);
                Assert.AreEqual("No call-back has been registered via the RegisterAuthorizationCallback method",
                                observer.Messages[0].Value.Exception.Message);
            }
        }

        public abstract class Given_a_registered_GoogleAuthorization : Given_a_newly_constructed_GoogleAuthorization
        {
            #region Setup guff

            private readonly GoogleResource[] _requestedResources = new[] { GoogleResource.Contacts };
            private const string _authUri = "http://someUri.com";

            private Given_a_registered_GoogleAuthorization()
            {
            }

            protected abstract RequestAuthorizationCode CreateCallback();

            public override void SetUp()
            {
                base.SetUp();
                var callback = CreateCallback();
                _sut.RegisterAuthorizationCallback(callback);
                _oAuthServiceMock.Setup(oaSvc => oaSvc.BuildAuthorizationUri(It.IsAny<Uri[]>()))
                                     .Returns(new Uri(_authUri));
            }

            #endregion

            [Test]
            public void Should_OnError_if_requesting_accesstoken_when_not_Authorized()
            {
                var observer = new TestScheduler().CreateObserver<string>();
                _sut.RequestAccessToken(GoogleResource.Contacts).Subscribe(observer);

                Assert.AreEqual(1, observer.Messages.Count);
                Assert.AreEqual(NotificationKind.OnError, observer.Messages[0].Value.Kind);
                Assert.IsInstanceOf<InvalidOperationException>(observer.Messages[0].Value.Exception);
                Assert.AreEqual("Can not request access token until authorized.", observer.Messages[0].Value.Exception.Message);
            }

            [Test]
            public void Should_OnError_if_no_Resources_have_been_selected()
            {
                var observer = new TestScheduler().CreateObserver<Unit>();
                _sut.Authorize(new GoogleResource[] { }).Subscribe(observer);

                Assert.AreEqual(1, observer.Messages.Count);
                Assert.AreEqual(NotificationKind.OnError, observer.Messages[0].Value.Kind);
                Assert.IsInstanceOf<InvalidOperationException>(observer.Messages[0].Value.Exception);
                Assert.AreEqual("No resources have been enabled.", observer.Messages[0].Value.Exception.Message);
            }

            [TestFixture]
            public sealed class When_calling_authorization_callback_with_empty_response : Given_a_registered_GoogleAuthorization
            {
                protected override RequestAuthorizationCode CreateCallback()
                {
                    RequestAuthorizationCode callback = uri => Observable.Empty<string>();
                    return callback;
                }

                [Test]
                public void Should_set_status_as_not_authorized()
                {
                    _sut.Authorize(_requestedResources).Subscribe();
                    Assert.IsFalse(_sut.Status.IsAuthorized);
                }

                [Test]
                public void Should_set_status_as_not_processing()
                {
                    _sut.Authorize(_requestedResources).Subscribe();
                    Assert.IsFalse(_sut.Status.IsProcessing);
                }

                [Test]
                public void Should_complete_authorization_sequence()
                {
                    var isCompleted = false;
                    _sut.Authorize(_requestedResources).Subscribe(_ => { }, () => { isCompleted = true; });
                    Assert.IsTrue(isCompleted);
                }
            }

            [TestFixture]
            public sealed class When_calling_authorization_callback_with_error_response : Given_a_registered_GoogleAuthorization
            {
                private Exception _expectedException;

                public override void SetUp()
                {
                    base.SetUp();
                    _expectedException = new Exception("Fail");
                }

                protected override RequestAuthorizationCode CreateCallback()
                {
                    RequestAuthorizationCode callback = uri => Observable.Throw<string>(_expectedException);
                    return callback;
                }

                [Test]
                public void Should_set_status_as_not_authorized()
                {
                    _sut.Authorize(_requestedResources).Subscribe(i => { }, ex => { });
                    Assert.IsFalse(_sut.Status.IsAuthorized);
                }

                [Test]
                public void Should_set_status_as_not_processing()
                {
                    _sut.Authorize(_requestedResources).Subscribe(i => { }, ex => { });
                    Assert.IsFalse(_sut.Status.IsProcessing);
                }

                [Test]
                public void Should_pass_through_error()
                {
                    Exception actual = null;
                    _sut.Authorize(_requestedResources).Subscribe(_ => { }, ex => { actual = ex; });
                    Assert.AreSame(_expectedException, actual);
                }
            }

            [TestFixture]
            public sealed class When_authorization_callback_responds_with_AuthToken : Given_a_registered_GoogleAuthorization
            {
                private const string _authCode = "ABCDEF";

                private Mock<ISession> _sessionMock;

                public override void SetUp()
                {
                    base.SetUp();

                    _sessionMock = new Mock<ISession>();
                    _oAuthServiceMock.Setup(oaSvc => oaSvc.RequestAccessToken(_authCode))
                                     .Returns(Observable.Return(_sessionMock.Object));
                }

                protected override RequestAuthorizationCode CreateCallback()
                {
                    RequestAuthorizationCode callback = uri => Observable.Return(_authCode);
                    return callback;
                }

                [Test]
                public void Should_set_status_as_authorized()
                {
                    _sut.Authorize(_requestedResources).Subscribe(i => { }, ex => { });
                    Assert.IsTrue(_sut.Status.IsAuthorized);
                }

                [Test]
                public void Should_set_status_as_not_processing()
                {
                    _sut.Authorize(_requestedResources).Subscribe(i => { }, ex => { });
                    Assert.IsFalse(_sut.Status.IsProcessing);
                }

                [Test]
                public void Should_complete_authorization_sequence()
                {
                    var isCompleted = false;
                    _sut.Authorize(_requestedResources).Subscribe(_ => { }, () => { isCompleted = true; });
                    Assert.IsTrue(isCompleted);
                }
            }
        }


        public abstract class Given_an_Authorized_GoogleAuthorization : Given_a_newly_constructed_GoogleAuthorization
        {
            #region Setup guff

            private readonly GoogleResource _authorizedResource = GoogleResource.Contacts;
            private readonly GoogleResource _unauthorizedResource = GoogleResource.Gmail;
            private string _accessToken = "TheAccessToken";
            private Given_an_Authorized_GoogleAuthorization()
            { }

            public override void SetUp()
            {
                base.SetUp();
                var authCode = "SomeAuthCode";
                var sessionMock = new Mock<ISession>();
                sessionMock.Setup(s => s.AccessToken).Returns(_accessToken);
                sessionMock.Setup(s => s.HasExpired()).Returns(false);

                _oAuthServiceMock.Setup(oaSvc => oaSvc.BuildAuthorizationUri(It.IsAny<Uri[]>()))
                                 .Returns(new Uri("http://someuri.com"));
                _oAuthServiceMock.Setup(oaSvc => oaSvc.RequestAccessToken(authCode))
                                 .Returns(Observable.Return(sessionMock.Object));

                RequestAuthorizationCode callback = uri => Observable.Return(authCode);
                _sut.RegisterAuthorizationCallback(callback);
                _sut.Authorize(new[] { _authorizedResource }).Subscribe();
            }

            #endregion

            [TestFixture]
            public sealed class When_requesting_an_unauthorized_access_token : Given_an_Authorized_GoogleAuthorization
            {
                [Test]
                public void Should_return_empty_sequence()
                {
                    var observer = new TestScheduler().CreateObserver<string>();
                    _sut.RequestAccessToken(_unauthorizedResource).Subscribe(observer);

                    Assert.AreEqual(1, observer.Messages.Count);
                    Assert.AreEqual(NotificationKind.OnCompleted, observer.Messages[0].Value.Kind);
                }
            }

            [TestFixture]
            public sealed class When_requesting_an_authorized_access_token : Given_an_Authorized_GoogleAuthorization
            {
                [Test]
                public void Should_return_access_token()
                {
                    var observer = new TestScheduler().CreateObserver<string>();
                    _sut.RequestAccessToken(_authorizedResource).Subscribe(observer);

                    Assert.AreEqual(2, observer.Messages.Count);
                    Assert.AreEqual(_accessToken, observer.Messages[0].Value.Value);
                    Assert.AreEqual(NotificationKind.OnCompleted, observer.Messages[1].Value.Kind);
                }

            }

            //  when requesting access token for modified resources
            //  when multiple concurrent requests for access token are made     
        }

        //Given a lapsed GoogleAuthoization


    }
}
// ReSharper restore InconsistentNaming