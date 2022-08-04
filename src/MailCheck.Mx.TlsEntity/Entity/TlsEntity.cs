using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MailCheck.Common.Contracts.Messaging;
using MailCheck.Common.Messaging.Abstractions;
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

namespace MailCheck.Mx.TlsEntity.Entity
{
    public class TlsEntity :
        IHandle<MxHostDeleted>,
        IHandle<TlsResultsEvaluated>,
        IHandle<MxHostTestPending>, 
        IHandle<TlsScheduledReminder>
    {
        private readonly ITlsEntityDao _dao;
        private readonly ISimplifiedTlsEntityDao _hostnameIpAddressDao;
        private readonly IClock _clock;
        private readonly ILogger<TlsEntity> _log;
        private readonly ITlsEntityConfig _tlsEntityConfig;
        private readonly IMessageDispatcher _dispatcher;
        private readonly IDomainStatusPublisher _domainStatusPublisher;
        private readonly IEntityChangedPublisher _entityChangedPublisher;
        private readonly IChangeNotifiersComposite _changeNotifiersComposite;
        private const string ServiceName = "Tls";
        private const string SimpleServiceName = "SimplifiedTls";

        public TlsEntity(ITlsEntityDao dao,
            IClock clock,
            ITlsEntityConfig tlsEntityConfig,
            IMessageDispatcher dispatcher,
            IDomainStatusPublisher domainStatusPublisher,
            IEntityChangedPublisher entityChangedPublisher,
            IChangeNotifiersComposite changeNotifiersComposite,
            ILogger<TlsEntity> log,
            ISimplifiedTlsEntityDao hostnameIpAddressDao)
        {
            _dao = dao;
            _clock = clock;
            _log = log;
            _domainStatusPublisher = domainStatusPublisher;
            _entityChangedPublisher = entityChangedPublisher;
            _tlsEntityConfig = tlsEntityConfig;
            _dispatcher = dispatcher;
            _changeNotifiersComposite = changeNotifiersComposite;
            _hostnameIpAddressDao = hostnameIpAddressDao;
        }

        public async Task Handle(MxHostTestPending message)
        {
            string hostname = message.Id.ToLower();
            List<string> ipAddresses = message.IpAddresses;

            await LoadOrCreateEntity(hostname);

            await _hostnameIpAddressDao.SyncIpAddressForHostname(hostname, ipAddresses);

            foreach(string ipAddress in ipAddresses)
            {
                CreateScheduledReminder createIpScheduledReminder = new CreateScheduledReminder(Guid.NewGuid().ToString(), SimpleServiceName, ipAddress, DateTime.UtcNow);
                _dispatcher.Dispatch(createIpScheduledReminder, _tlsEntityConfig.SnsTopicArn);
                _log.LogInformation($"A CreateScheduledReminder message for Host: {hostname} / ipAddress: {ipAddress} at time: {createIpScheduledReminder.ScheduledTime} has been dispatched to SnsTopic: {_tlsEntityConfig.SnsTopicArn}");
            }

            CreateScheduledReminder createScheduledReminder = new CreateScheduledReminder(Guid.NewGuid().ToString(), ServiceName, hostname, DateTime.UtcNow);         
            _dispatcher.Dispatch(createScheduledReminder, _tlsEntityConfig.SnsTopicArn);
            _log.LogInformation($"A CreateScheduledReminder message for Host: {hostname} at time: {createScheduledReminder.ScheduledTime} has been dispatched to SnsTopic: {_tlsEntityConfig.SnsTopicArn}");
        }

        public Task Handle(TlsScheduledReminder message)
        {
            _log.LogInformation($"A TlsScheduledReminder message for host: { message.ResourceId.ToLower()} has been dropped");

            return Task.CompletedTask;
        }

        public async Task Handle(TlsResultsEvaluated message)
        {
            string hostname = message.Id.ToLower();

            message.TlsRecords = new TlsRecords(null);
            if (message.Certificates != null) message.Certificates.Errors = new List<Error>();

            TlsEntityState state = await LoadOrCreateEntity(hostname);

            state.TlsState = TlsState.Evaluated;
            state.FailureCount = message.Failed ? state.FailureCount + 1 : 0;

            if (!message.Failed || message.Failed && state.FailureCount >= _tlsEntityConfig.MaxTlsRetryAttempts)
            {
                List<string> domains = await _dao.GetDomainsFromHost(hostname);
                _changeNotifiersComposite.Handle(state, message, domains);

                state.CertificateResults = message.Certificates;
                state.TlsRecords = message.TlsRecords;
                state.LastUpdated = message.Timestamp;

                await _dao.Save(state);

                await _domainStatusPublisher.Publish(hostname);
                _entityChangedPublisher.Publish(hostname, state, nameof(TlsResultsEvaluated));
            }
            else
            {
                await _dao.Save(state);
            }

            ReminderSuccessful reminderSuccessful = new ReminderSuccessful(
                Guid.NewGuid().ToString(),
                ServiceName,
                hostname,
                _clock.GetDateTimeUtc());

            _dispatcher.Dispatch(reminderSuccessful, _tlsEntityConfig.SnsTopicArn);
            _log.LogInformation($"A ReminderSuccessful message for Domain: {hostname} has been dispatched to SnsTopic: {_tlsEntityConfig.SnsTopicArn}");
        }
        
        public async Task Handle(MxHostDeleted message)
        {
            string hostname = message.Id.ToLower();
            await _dao.Delete(hostname);
            _log.LogInformation($"Deleted TLS entity with id: {message.Id}.");
            
            DeleteScheduledReminder deleteScheduledReminder = new DeleteScheduledReminder(
                Guid.NewGuid().ToString(),
                ServiceName,
                hostname);
                
            _dispatcher.Dispatch(deleteScheduledReminder, _tlsEntityConfig.SnsTopicArn);
            _log.LogInformation($"A DeleteScheduledReminder message for Domain: {hostname} has been dispatched to SnsTopic: {_tlsEntityConfig.SnsTopicArn}");
        }

        private async Task<TlsEntityState> LoadOrCreateEntity(string hostname)
        {
            TlsEntityState state = await _dao.Get(hostname);

            if (state == null)
            {
                state = new TlsEntityState(hostname);
                await _dao.Save(state);
                _log.LogInformation($"TlsEntityState created for {hostname}.");
            }

            return state;
        }
    }
}
