using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MailCheck.Common.Contracts.Advisories;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Mx.TlsEntity.Config;
using MailCheck.Mx.TlsEntity.Entity.Notifications;
using Microsoft.Extensions.Logging;

namespace MailCheck.Mx.TlsEntity.Entity.Notifiers
{
    public interface ISimplifiedAdvisoryChangedNotifier<TMessageFactory> 
        where TMessageFactory : IFactory, new()
    {
        void Notify(string host, List<string> domains, IEnumerable<AdvisoryMessage> currentAdvisories, IEnumerable<AdvisoryMessage> newAdvisories);
    }

    public class SimplifiedAdvisoryChangedNotifier<TMessageFactory> : ISimplifiedAdvisoryChangedNotifier<TMessageFactory>
        where TMessageFactory : IFactory, new()
    {
        private static readonly TMessageFactory Factory = new TMessageFactory();

        private readonly IMessageDispatcher _messageDispatcher;
        private readonly ITlsEntityConfig _tlsEntityConfig;
        private readonly ILogger<SimplifiedAdvisoryChangedNotifier<TMessageFactory>> _log;

        public SimplifiedAdvisoryChangedNotifier(
            IMessageDispatcher messageDispatcher,
            ITlsEntityConfig tlsEntityConfig,
            ILogger<SimplifiedAdvisoryChangedNotifier<TMessageFactory>> log)
        {
            _messageDispatcher = messageDispatcher;
            _tlsEntityConfig = tlsEntityConfig;
            _log = log;
        }

        public void Notify(string host, List<string> domains, IEnumerable<AdvisoryMessage> currentAdvisories, IEnumerable<AdvisoryMessage> newAdvisories)
        {
            currentAdvisories = ExcludeSuccess(currentAdvisories);
            newAdvisories = ExcludeSuccess(newAdvisories);

            Advisories<AdvisoryMessage> advisories = new Advisories<AdvisoryMessage>(currentAdvisories, newAdvisories);

            if (advisories.Added.Count > 0)
            {
                domains.ForEach(domain => _messageDispatcher.Dispatch(Factory.CreateAddedMessage(domain, host, advisories.Added), _tlsEntityConfig.SnsTopicArn));
                _log.LogDebug($"Dispatched {domains.Count} {Factory.Name}AdvisoryAdded messages which contain {advisories.Added.Count} advisories");
            }

            if (advisories.Sustained.Count > 0)
            {
                domains.ForEach(domain => _messageDispatcher.Dispatch(Factory.CreateSustainedMessage(domain, host, advisories.Sustained), _tlsEntityConfig.SnsTopicArn));
                _log.LogDebug($"Dispatched {domains.Count} {Factory.Name}AdvisorySustained messages which contain {advisories.Sustained.Count} advisories");
            }

            if (advisories.Removed.Count > 0)
            {
                domains.ForEach(domain => _messageDispatcher.Dispatch(Factory.CreateRemovedMessage(domain, host, advisories.Removed), _tlsEntityConfig.SnsTopicArn));
                _log.LogDebug($"Dispatched {domains.Count} {Factory.Name}AdvisoryRemoved messages which contain {advisories.Removed.Count} advisories");
            }
        }

        private static IEnumerable<AdvisoryMessage> ExcludeSuccess(IEnumerable<AdvisoryMessage> advisories)
        {
            return advisories
                .Where(tlsEvalResult => !string.IsNullOrWhiteSpace(tlsEvalResult.Text))
                .Where(tlsEvalResult => tlsEvalResult.MessageType != MessageType.success);
        }
    }

    public interface IFactory
    {
        string Name { get; }
        Message CreateAddedMessage(string id, string host, List<AdvisoryMessage> advisories);
        Message CreateSustainedMessage(string id, string host, List<AdvisoryMessage> advisories);
        Message CreateRemovedMessage(string id, string host, List<AdvisoryMessage> advisories);
    }

    public class TlsFactory : IFactory
    {
        public string Name { get; } = "Tls";

        public Message CreateAddedMessage(string id, string host, List<AdvisoryMessage> advisories)
        {
            return new TlsAdvisoryAdded(id, host, advisories);
        }

        public Message CreateRemovedMessage(string id, string host, List<AdvisoryMessage> advisories)
        {
            return new TlsAdvisoryRemoved(id, host, advisories);
        }

        public Message CreateSustainedMessage(string id, string host, List<AdvisoryMessage> advisories)
        {
            return new TlsAdvisorySustained(id, host, advisories);
        }
    }

    public class CertFactory : IFactory
    {
        public string Name { get; } = "TlsCert";

        public Message CreateAddedMessage(string id, string host, List<AdvisoryMessage> advisories)
        {
            return new TlsCertAdvisoryAdded(id, host, advisories);
        }

        public Message CreateRemovedMessage(string id, string host, List<AdvisoryMessage> advisories)
        {
            return new TlsCertAdvisoryRemoved(id, host, advisories);
        }

        public Message CreateSustainedMessage(string id, string host, List<AdvisoryMessage> advisories)
        {
            return new TlsCertAdvisorySustained(id, host, advisories);
        }
    }
}
