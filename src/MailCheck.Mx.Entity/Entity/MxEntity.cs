using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MailCheck.Common.Contracts.Findings;
using MailCheck.Common.Contracts.Messaging;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Common.Util;
using MailCheck.Mx.Contracts.Entity;
using MailCheck.Mx.Contracts.External;
using MailCheck.Mx.Contracts.Poller;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Simplified;
using MailCheck.Mx.Entity.Config;
using MailCheck.Mx.Entity.Dao;
using MailCheck.Mx.Entity.Entity.Notifiers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Message = MailCheck.Common.Messaging.Abstractions.Message;

namespace MailCheck.Mx.Entity.Entity
{
    public class MxEntity :
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
        private readonly IFindingFactory _findingFactory;
        private const string ServiceName = "Mx";

        public MxEntity(IMxEntityDao dao,
            IMxEntityConfig mxEntityConfig,
            IMessageDispatcher dispatcher,
            IChangeNotifiersComposite changeNotifiersComposite,
            IClock clock,
            IFindingFactory findingFactory,
            ILogger<MxEntity> log)
        {
            _dao = dao;
            _log = log;
            _findingFactory = findingFactory;
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

            Message createScheduledReminder = new CreateScheduledReminder(
                Guid.NewGuid().ToString(),
                ServiceName,
                domainName,
                default);

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
            MxEntityState state = await _dao.Get(domainName);

            if (state == null)
            {
                _log.LogInformation($"Ignoring {nameof(MxScheduledReminder)} as MxEntity does not exist for {domainName}.");
                return;
            }

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

            MxEntityState state = await _dao.Get(domainName);

            if (state == null)
            {
                _log.LogInformation($"Ignoring {nameof(DomainDeleted)} as MxEntity does not exist for {domainName}.");
                return;
            }

            if (state.HostMxRecords != null && state.HostMxRecords.Count > 0)
            {
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

            Message deleteScheduledReminder = new DeleteScheduledReminder(
                Guid.NewGuid().ToString(),
                ServiceName,
                domainName);

            _dispatcher.Dispatch(deleteScheduledReminder, _mxEntityConfig.SnsTopicArn);
            _log.LogInformation(
                $"A DeleteScheduledReminder message for Domain: {domainName} has been dispatched to SnsTopic: {_mxEntityConfig.SnsTopicArn}");
        }

        public async Task Handle(MxRecordsPolled message)
        {
            string domainName = message.Id.ToLower();

            MxEntityState state = await _dao.Get(domainName);

            if (state == null)
            {
                _log.LogInformation($"Ignoring {nameof(MxRecordsPolled)} as MxEntity does not exist for {domainName}.");
                return;
            }

            MxState oldState = state.MxState;
            int oldRecordCount = state.HostMxRecords?.Count ?? 0;
            int newRecordCount = message.Records?.Count ?? 0;

            _changeNotifiersComposite.Handle(state, message);
            await VerifySavedHosts(state, message);

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

            state.HostMxRecords = validHostRecords;
            state.LastUpdated = message.Timestamp;
            state.Error = message.Error;
            state.MxState = MxState.Evaluated;

            await _dao.Save(state);
            _log.LogInformation($"Updated MxEntity from {oldState} to {MxState.Evaluated} and MX records before: {oldRecordCount} after: {newRecordCount} for {domainName}.");

            Message mxRecordsUpdated = new MxRecordsUpdated(domainName, validHostRecords);
            _dispatcher.Dispatch(mxRecordsUpdated, _mxEntityConfig.SnsTopicArn);
            _log.LogInformation($"An MxRecordsUpdated message for Domain: {domainName} has been dispatched to SnsTopic: {_mxEntityConfig.SnsTopicArn}");

            // Should probably change this so it only happens for a new host
            validHostRecords?.ForEach(mxRecord =>
            {
                Message message = new MxHostTestPending(mxRecord.Id, mxRecord.IpAddresses);
                _dispatcher.Dispatch(message, _mxEntityConfig.SnsTopicArn);
                _log.LogInformation($"An MxTestPending message for Host: {mxRecord.Id} has been dispatched to SnsTopic: {_mxEntityConfig.SnsTopicArn}");
            });

            Message reminderSuccessful = new ReminderSuccessful(
                Guid.NewGuid().ToString(),
                ServiceName,
                domainName,
                _clock.GetDateTimeUtc());

            _dispatcher.Dispatch(reminderSuccessful, _mxEntityConfig.SnsTopicArn);
            _log.LogInformation($"A ReminderSuccessful message for Domain: {domainName} has been dispatched to SnsTopic: {_mxEntityConfig.SnsTopicArn}");

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

        private async Task VerifySavedHosts(MxEntityState state, MxRecordsPolled polled)
        {
            string domain = state.Id;
            IEnumerable<string> oldHostsForDomain = state.HostMxRecords?.Select(x => x.Id) ?? new List<string>();
            IEnumerable<string> newHostsForDomain = polled.Records?.Select(x => x.Id) ?? new List<string>();

            IEnumerable<string> removedHosts = oldHostsForDomain.Except(newHostsForDomain).ToList();

            if (!removedHosts.Any())
            {
                _log.LogInformation($"Hosts remain the same for domain {domain}");
                return;
            }

            List<Finding> findingsToRemove = new List<Finding>();
            foreach (string removedHost in removedHosts)
            {
                _log.LogInformation($"Host {removedHost} removed for domain {domain}");
                List<SimplifiedTlsEntityState> tlsEntityState = await _dao.GetSimplifiedStates(removedHost);

                IEnumerable<NamedAdvisory> tlsAdvisories = tlsEntityState.SelectMany(x => x.TlsAdvisories ?? Enumerable.Empty<NamedAdvisory>());
                IEnumerable<NamedAdvisory> certAdvisories = tlsEntityState.SelectMany(x => x.CertAdvisories ?? Enumerable.Empty<NamedAdvisory>());

                IEnumerable<Finding> findings = tlsAdvisories.Concat(certAdvisories).Select(x => _findingFactory.Create(x, domain, removedHost));
                findingsToRemove.AddRange(findings);
            }

            if (!findingsToRemove.Any())
            {
                _log.LogInformation($"No TLS findings to remove for {domain}");
                return;
            }

            _log.LogInformation($"Hosts removed for domain {domain} - dispatching FindingsChanged with {findingsToRemove.Count} findings to remove: {Environment.NewLine}{JsonConvert.SerializeObject(findingsToRemove)}");

            FindingsChanged cleanupMessage = new FindingsChanged(Guid.NewGuid().ToString())
            {
                Domain = domain,
                RecordType = "TLS",
                Removed = findingsToRemove
            };

            _dispatcher.Dispatch(cleanupMessage, _mxEntityConfig.SnsTopicArn);
            _log.LogInformation($"A FindingsChanged message for Domain: {domain} has been dispatched to SnsTopic: {_mxEntityConfig.SnsTopicArn}");
        }
    }
}