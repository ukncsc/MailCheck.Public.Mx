using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using MailCheck.Common.Contracts.Messaging;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Common.Messaging.Common.Exception;
using MailCheck.Common.Util;
using MailCheck.Mx.Contracts.Entity;
using MailCheck.Mx.Contracts.External;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.TlsEntity;
using MailCheck.Mx.Contracts.TlsEvaluator;
using MailCheck.Mx.TlsEntity.Config;
using MailCheck.Mx.TlsEntity.Dao;
using MailCheck.Mx.TlsEntity.Entity.DomainStatus;
using MailCheck.Mx.TlsEntity.Entity.EmailSecurity;
using MailCheck.Mx.TlsEntity.Entity.Notifiers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MailCheck.Mx.TlsEntity.Entity
{
    public class TlsEntityNewScheduler :
        IHandle<MxHostDeleted>,
        IHandle<TlsScheduledReminder>,
        IHandle<TlsResultsEvaluated>,
        IHandle<MxHostTestPending>
    {
        private readonly ITlsEntityDao _dao;
        private readonly IClock _clock;
        private readonly ILogger<TlsEntityNewScheduler> _log;
        private readonly ITlsEntityConfig _tlsEntityConfig;
        private readonly IMessageDispatcher _dispatcher;
        private readonly IDomainStatusPublisher _domainStatusPublisher;
        private readonly IEntityChangedPublisher _entityChangedPublisher;
        private readonly IChangeNotifiersComposite _changeNotifiersComposite;
        private const string ServiceName = "Tls";

        public TlsEntityNewScheduler(ITlsEntityDao dao,
            IClock clock,
            ITlsEntityConfig tlsEntityConfig,
            IMessageDispatcher dispatcher,
            IDomainStatusPublisher domainStatusPublisher,
            IEntityChangedPublisher entityChangedPublisher,
            IChangeNotifiersComposite changeNotifiersComposite,
            ILogger<TlsEntityNewScheduler> log)
        {
            _dao = dao;
            _clock = clock;
            _log = log;
            _domainStatusPublisher = domainStatusPublisher;
            _entityChangedPublisher = entityChangedPublisher;
            _tlsEntityConfig = tlsEntityConfig;
            _dispatcher = dispatcher;
            _changeNotifiersComposite = changeNotifiersComposite;
        }
        
        public async Task Handle(TlsScheduledReminder message)
        {
            await HandleReadyToPoll(message.ResourceId.ToLower(), nameof(message));
        }

        public async Task Handle(MxHostTestPending message)
        {
            await HandleReadyToPoll(message.Id.ToLower(), nameof(message));
        }

        private async Task HandleReadyToPoll(string hostName, string messageType)
        {
            TlsEntityState state = await LoadState(hostName, messageType);

            if (state.LastUpdated == null || _clock.GetDateTimeUtc() > state.LastUpdated.Value.AddSeconds(_tlsEntityConfig.MinimumSchedulerInterval))
            {
                state.TlsState = TlsState.PollPending;

                await _dao.Save(state);

                _dispatcher.Dispatch(new TlsTestPending(hostName), _tlsEntityConfig.SnsTopicArn);
                _log.LogInformation($"A TlsTestPending message for host: {hostName} has been dispatched to SnsTopic: {_tlsEntityConfig.SnsTopicArn}");
            }
            else
            {
                _log.LogInformation($"A request to re-test {hostName} was ignored as it was last tested at {state.LastUpdated.Value} and the re-test cache " +
                                    $"is {TimeSpan.FromSeconds(_tlsEntityConfig.TlsResultsCacheInSeconds):dd\\d\\:hh\\h\\:mm\\m\\:ss\\s}."); // dd/hh/mm/ss
            }
        }

        public async Task Handle(TlsResultsEvaluated message)
        {
            string messageId = message.Id.ToLower();

            TlsEntityState state = await LoadState(messageId, nameof(message));

            await _domainStatusPublisher.Publish(message);
            _entityChangedPublisher.Publish(messageId, state, nameof(TlsResultsEvaluated));

            state.TlsState = TlsState.Evaluated;
            state.FailureCount = message.Failed ? state.FailureCount + 1 : 0;

            if (!message.Failed ||
                message.Failed && state.FailureCount >= _tlsEntityConfig.MaxTlsRetryAttempts)
            {
                List<string> domains = await _dao.GetDomainsFromHost(messageId);
                _changeNotifiersComposite.Handle(state, message, domains);

                state.CertificateResults = message.Certificates;
                state.TlsRecords = message.TlsRecords;
                state.LastUpdated = message.Timestamp;

                await _dao.Save(state);

                _dispatcher.Dispatch(new TlsRecordEvaluationsChanged(messageId, state.TlsRecords, state.CertificateResults),
                    _tlsEntityConfig.SnsTopicArn);
            }
            else
            {
                await _dao.Save(state);
            }

            ReminderSuccessful reminderSuccessful = new ReminderSuccessful(
                Guid.NewGuid().ToString(),
                ServiceName,
                messageId,
                _clock.GetDateTimeUtc());

            _dispatcher.Dispatch(reminderSuccessful, _tlsEntityConfig.SnsTopicArn);
            _log.LogInformation($"A ReminderSuccessful message for Domain: {messageId} has been dispatched to SnsTopic: {_tlsEntityConfig.SnsTopicArn}");
        }
        
        public async Task Handle(MxHostDeleted message)
        {
            string domainName = message.Id.ToLower();
            await _dao.Delete(domainName);
            _log.LogInformation($"Deleted TLS entity with id: {message.Id}.");
            
            DeleteScheduledReminder deleteScheduledReminder = new DeleteScheduledReminder(
                Guid.NewGuid().ToString(),
                ServiceName,
                domainName);
                
            _dispatcher.Dispatch(deleteScheduledReminder, _tlsEntityConfig.SnsTopicArn);
            _log.LogInformation($"A DeleteScheduledReminder message for Domain: {domainName} has been dispatched to SnsTopic: {_tlsEntityConfig.SnsTopicArn}");
        }

        private async Task<TlsEntityState> LoadState(string id, string messageType)
        {
            TlsEntityState state = await _dao.Get(id);

            if (state == null)
            {
                state = new TlsEntityState(id);

                _log.LogInformation("Processing {EventName} - Tls Entity State does not exist for {Id}, creating new state.", messageType, id);
                await _dao.Save(state);
                _log.LogInformation("Tls Entity State created for {Id}.", id);

                TlsEntityCreated mxEntityCreated = new TlsEntityCreated(id);
                _dispatcher.Dispatch(mxEntityCreated, _tlsEntityConfig.SnsTopicArn);

                _log.LogInformation("Created _tlsEntityConfig for {Id}.", id);

                CreateScheduledReminder createScheduledReminder = new CreateScheduledReminder(
                    Guid.NewGuid().ToString(),
                    ServiceName,
                    id,
                    default);
                
                _dispatcher.Dispatch(createScheduledReminder, _tlsEntityConfig.SnsTopicArn);
                _log.LogInformation($"A CreateScheduledReminder message for Domain: {id} has been dispatched to SnsTopic: {_tlsEntityConfig.SnsTopicArn}");
            }
            
            return state;
        }
    }
}
