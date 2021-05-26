using System;
using MailCheck.Common.Contracts.Messaging;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Mx.Contracts.Entity;
using MailCheck.Mx.TlsEntity.Config;
using Microsoft.Extensions.Logging;

namespace MailCheck.Mx.TlsEntity.Entity.EmailSecurity
{
    public interface IEntityChangedPublisher
    {
        void Publish(string domain, TlsEntityState state, string reason);
    }

    public class EntityChangedPublisher : IEntityChangedPublisher
    {
        private readonly ITlsEntityConfig _config;
        private readonly IMessageDispatcher _dispatcher;
        private readonly ILogger<EntityChangedPublisher> _log;

        public EntityChangedPublisher (
            ITlsEntityConfig config,
            IMessageDispatcher dispatcher,
            ILogger<EntityChangedPublisher> log)
        {
            _config = config;
            _dispatcher = dispatcher;
            _log = log;
        }

        public void Publish(string domain, TlsEntityState state, string reason)
        {
            EntityChanged message = new EntityChanged(domain)
            {
                RecordType = _config.RecordType,
                NewEntityDetail = state,
                ChangedAt = DateTime.UtcNow,
                ReasonForChange = reason
            };

            _dispatcher.Dispatch(message, _config.SnsTopicArn);
            _log.LogInformation($"EntityChanged message dispatched for ${domain}");
        }
    }
}