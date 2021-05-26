using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MailCheck.Common.Contracts.Messaging;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Common.Messaging.Common.Exception;
using MailCheck.Common.Util;
using MailCheck.Mx.Contracts.Entity;
using MailCheck.Mx.Contracts.External;
using MailCheck.Mx.Contracts.Poller;
using MailCheck.Mx.Entity.Config;
using MailCheck.Mx.Entity.Dao;
using MailCheck.Mx.Entity.Entity.Notifiers;
using Microsoft.Extensions.Logging;

namespace MailCheck.Mx.Entity.Entity
{
    public class MxEntity:
        IHandle<DomainCreated>,
        IHandle<MxScheduledReminder>,
        IHandle<DomainDeleted>,
        IHandle<MxRecordsPolled>
    {
        private readonly IMxEntityDao _dao;
        private readonly IMxEntityConfig _mxEntityConfig;
        private readonly IMessageDispatcher _dispatcher;
        private readonly IChangeNotifiersComposite _changeNotifiersComposite;
        private readonly IClock _clock;
        private readonly ILogger<MxEntity> _log;

        public MxEntity(IMxEntityDao dao,
            IMxEntityConfig mxEntityConfig,
            IMessageDispatcher dispatcher, 
            IChangeNotifiersComposite changeNotifiersComposite, 
            IClock clock,
            ILogger<MxEntity> log)
        {
            _dao = dao;
            _log = log;
            _clock = clock;
            _mxEntityConfig = mxEntityConfig;
            _dispatcher = dispatcher;
            _changeNotifiersComposite = changeNotifiersComposite;
        }

        public async Task Handle(DomainCreated message)
        {
            string domainName = message.Id.ToLower();

            MxEntityState state = await _dao.Get(domainName);

            if (state != null)
            {
                _log.LogInformation($"Ignoring {nameof(DomainCreated)} as MxEntity already exists for {domainName}.");
                return;
            }

            await _dao.Save(new MxEntityState(domainName));
            _log.LogInformation($"Created MxEntity for {domainName}.");

            MxEntityCreated mxEntityCreated = new MxEntityCreated(domainName);
            _dispatcher.Dispatch(mxEntityCreated, _mxEntityConfig.SnsTopicArn);
            _log.LogInformation(
                $"An MxEntityCreated message for Domain: {domainName} has been dispatched to SnsTopic: {_mxEntityConfig.SnsTopicArn}");

            Message createScheduledReminder = new CreateScheduledReminder(Guid.NewGuid().ToString(), "Mx", domainName, _clock.GetDateTimeUtc());
            _dispatcher.Dispatch(createScheduledReminder, _mxEntityConfig.SnsTopicArn);
            _log.LogInformation(
                $"A CreateScheduledReminder message for Domain: {domainName} has been dispatched to SnsTopic: {_mxEntityConfig.SnsTopicArn}");

            Message entityChanged = new EntityChanged(domainName)
            {
                RecordType = "MX",
                ChangedAt = DateTime.UtcNow,
                NewEntityDetail = state,
                ReasonForChange = nameof(DomainCreated)
            };
            _dispatcher.Dispatch(entityChanged, _mxEntityConfig.SnsTopicArn);
            _log.LogInformation(
                $"An EntityChanged message for Domain: {domainName} has been dispatched to SnsTopic: {_mxEntityConfig.SnsTopicArn}");
        }
        
        public async Task Handle(MxScheduledReminder message)
        {
            string domainName = message.ResourceId.ToLower();
            MxEntityState state = await LoadState(domainName, nameof(message));

            await _dao.UpdateState(domainName, MxState.PollPending);
            _log.LogInformation($"Updated MxEntity.MxState from {state.MxState} to {MxState.PollPending} for {domainName}.");

            Message mxPollPending = new MxPollPending(domainName);
            _dispatcher.Dispatch(mxPollPending, _mxEntityConfig.SnsTopicArn);
            _log.LogInformation(
                $"An MxPollPending message for Domain: {domainName} has been dispatched to SnsTopic: {_mxEntityConfig.SnsTopicArn}");
        }

        public async Task Handle(DomainDeleted message)
        {
            string domainName = message.Id.ToLower();

            MxEntityState state = await LoadState(domainName, nameof(message));

            if (state.HostMxRecords != null && state.HostMxRecords.Count > 0) {
                List<string> uniqueHosts = await _dao.GetHostsUniqueToDomain(domainName);
                if (uniqueHosts.Count > 0)
                {
                    await _dao.DeleteHosts(uniqueHosts);
                    foreach (string host in uniqueHosts)
                    {
                        _dispatcher.Dispatch(new MxHostDeleted(host), _mxEntityConfig.SnsTopicArn);
                        _log.LogInformation($"An MxHostDeleted message for Host: {host} has been dispatched to the SnsTopic: {_mxEntityConfig.SnsTopicArn}");
                    }
                }
            }

            await _dao.Delete(domainName);
            _log.LogInformation($"Deleted MX entity with id: {message.Id}.");

            Message entityChanged = new EntityChanged(domainName)
            {
                RecordType = "MX",
                ChangedAt = DateTime.UtcNow,
                NewEntityDetail = state,
                ReasonForChange = nameof(DomainDeleted)
            };
            _dispatcher.Dispatch(entityChanged, _mxEntityConfig.SnsTopicArn);
            _log.LogInformation(
                $"An EntityChanged message for Domain: {domainName} has been dispatched to SnsTopic: {_mxEntityConfig.SnsTopicArn}");
        }

        public async Task Handle(MxRecordsPolled message)
        {
            string domainName = message.Id.ToLower();

            MxEntityState state = await LoadState(domainName, nameof(message));

            MxState oldState = state.MxState;
            int oldRecordCount = state.HostMxRecords?.Count ?? 0;
            int newRecordCount = message.Records?.Count ?? 0;

            _changeNotifiersComposite.Handle(state, message);

            List<HostMxRecord> validHostRecords = new List<HostMxRecord>();
            if (message.Records != null)
            {
                foreach (HostMxRecord hostRecord in message.Records)
                {
                    if (Uri.CheckHostName(hostRecord.Id) != UriHostNameType.Unknown)
                    {
                        validHostRecords.Add(hostRecord);
                    }
                    else
                    {
                        _log.LogInformation($"Erroneous host: {hostRecord.Id} found for domain: {domainName}");
                    }
                }
            }

            if (message.Error == null)
            {
                state.HostMxRecords = validHostRecords;
            }

            state.LastUpdated = message.Timestamp;
            state.Error = message.Error;
            state.MxState = MxState.Evaluated;

            await _dao.Save(state);
            _log.LogInformation($"Updated MxEntity from {oldState} to {MxState.Evaluated} and MX records before: {oldRecordCount} after: {newRecordCount} for {domainName}.");

            Message mxRecordsUpdated = new MxRecordsUpdated(domainName, validHostRecords);
            _dispatcher.Dispatch(mxRecordsUpdated, _mxEntityConfig.SnsTopicArn);
            _log.LogInformation($"An MxRecordsUpdated message for Domain: {domainName} has been dispatched to SnsTopic: {_mxEntityConfig.SnsTopicArn}");

            // Should probably change this so it only happens for a new host
            validHostRecords?.ForEach(mxRecord => {
                _dispatcher.Dispatch(new MxHostTestPending(mxRecord.Id), _mxEntityConfig.SnsTopicArn);
                _log.LogInformation($"An MxHostTestPending message for Host: {mxRecord.Id} has been dispatched to SnsTopic: {_mxEntityConfig.SnsTopicArn}");
            });

            Message createScheduledReminder = new CreateScheduledReminder(
                Guid.NewGuid().ToString(),
                "Mx",
                domainName,
                _clock.GetDateTimeUtc().AddSeconds(_mxEntityConfig.NextScheduledInSeconds * (1 - new Random().NextDouble() * 0.25))
            );
            
            _dispatcher.Dispatch(createScheduledReminder, _mxEntityConfig.SnsTopicArn);
            _log.LogInformation($"A CreateScheduledReminder message for Domain: {domainName} has been dispatched to SnsTopic: {_mxEntityConfig.SnsTopicArn}");

            Message entityChanged = new EntityChanged(domainName)
            {
                RecordType = "MX",
                ChangedAt = DateTime.UtcNow,
                NewEntityDetail = state,
                ReasonForChange = nameof(MxRecordsPolled)
            };
            _dispatcher.Dispatch(entityChanged, _mxEntityConfig.SnsTopicArn);
            _log.LogInformation(
                $"An EntityChanged message for Domain: {domainName} has been dispatched to SnsTopic: {_mxEntityConfig.SnsTopicArn}");
        }

        private async Task<MxEntityState> LoadState(string domainName, string messageType)
        {
            MxEntityState state = await _dao.Get(domainName);

            if (state == null)
            {
                _log.LogError("Ignoring {EventName} as MX Entity does not exist for {Id}.", messageType, domainName);
                throw new MailCheckException(
                    $"Cannot handle event {messageType} as MX Entity doesnt exists for {domainName}.");
            }

            return state;
        }
    }
}
