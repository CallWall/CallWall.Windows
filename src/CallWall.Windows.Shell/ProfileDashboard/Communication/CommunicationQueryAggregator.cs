﻿using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using CallWall.Windows.Contract;
using CallWall.Windows.Contract.Communication;

namespace CallWall.Windows.Shell.ProfileDashboard.Communication
{
    public sealed class CommunicationQueryAggregator : ICommunicationQueryAggregator
    {
        private readonly ILogger _logger;
        private readonly IEnumerable<ICommunicationQueryProvider> _communicationQueryProviders;

        public CommunicationQueryAggregator(ILoggerFactory loggerFactory, IEnumerable<ICommunicationQueryProvider> communicationQueryProviders)
        {
            _logger = loggerFactory.CreateLogger(GetType());
            _communicationQueryProviders = communicationQueryProviders;
        }

        public IObservable<Message> Search(IProfile activeProfile)
        {
            return from provider in _communicationQueryProviders.ToObservable()
                   from message in provider.LoadMessages(activeProfile)
                                           .Log(_logger, "provider.LoadMessages(activeProfile)")
                                           .Catch<IMessage, Exception>(ex =>
                                            {
                                                _logger.Error(ex, "{0} failed loading messages", provider.GetType().Name);
                                                return Observable.Empty<IMessage>();
                                            })
                   select new Message(message);
        }
    }
}
