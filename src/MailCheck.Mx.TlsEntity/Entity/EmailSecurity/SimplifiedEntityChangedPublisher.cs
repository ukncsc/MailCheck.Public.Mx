using MailCheck.Common.Contracts.Messaging;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Mx.TlsEntity.Config;
using MailCheck.Mx.Contracts.Simplified;
using Microsoft.Extensions.Logging;
using MailCheck.Common.Util;

namespace MailCheck.Mx.TlsEntity.Entity.EmailSecurity
{
    public interface ISimplifiedEntityChangedPublisher
    {
        void Publish(string domain, SimplifiedEmailSecTlsEntityState state, string reason);
    }

    public class SimplifiedEntityChangedPublisher : ISimplifiedEntityChangedPublisher
    {
        private readonly ITlsEntityConfig _config;
        private readonly IMessageDispatcher _dispatcher;
        private readonly ILogger<EntityChangedPublisher> _log;
        private readonly IClock _clock;

        public SimplifiedEntityChangedPublisher (
            ITlsEntityConfig config,
            IMessageDispatcher dispatcher,
            ILogger<EntityChangedPublisher> log,
            IClock clock)
        {
            _config = config;
            _dispatcher = dispatcher;
            _log = log;
            _clock = clock;
        }

        public void Publish(string domain, SimplifiedEmailSecTlsEntityState state, string reason)
        {
            EntityChanged message = new EntityChanged(domain)
            {
                RecordType = _config.SimplifiedRecordType,
                NewEntityDetail = state,
                ChangedAt = _clock.GetDateTimeUtc(),
                ReasonForChange = reason
            };

            _dispatcher.Dispatch(message, _config.SnsTopicArn);
            _log.LogDebug($"EntityChanged message dispatched for ${domain}");
        }
    }
}