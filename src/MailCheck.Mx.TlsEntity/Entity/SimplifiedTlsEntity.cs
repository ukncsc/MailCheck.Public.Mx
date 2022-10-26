using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MailCheck.Common.Contracts.Messaging;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Mx.Contracts.External;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Simplified;
using MailCheck.Mx.TlsEntity.Config;
using MailCheck.Mx.TlsEntity.Dao;
using MailCheck.Mx.TlsEntity.Entity.DomainStatus;
using MailCheck.Mx.TlsEntity.Entity.Notifiers;
using MailCheck.Mx.TlsEntity.Entity.EmailSecurity;
using Microsoft.Extensions.Logging;
using MoreLinq;

namespace MailCheck.Mx.TlsEntity.Entity
{
    public class SimplifiedTlsEntity :
        IHandle<SimplifiedTlsScheduledReminder>,
        IHandle<SimplifiedTlsTestResults>,
        IHandle<SimplifiedHostCertificateEvaluated>,
        IHandle<SimplifiedTlsExpired>
    {
        private const string ServiceName = "SimplifiedTls";

        private readonly ILogger<SimplifiedTlsEntity> _log;
        private readonly ITlsEntityConfig _tlsEntityConfig;
        private readonly IMessageDispatcher _dispatcher;
        private readonly ISimplifiedTlsEntityDao _simplifiedTlsEntityDao;
        private readonly ISimplifiedAdvisoryChangedNotifier<TlsFactory> _tlsChangeNotifier;
        private readonly ISimplifiedAdvisoryChangedNotifier<CertFactory> _certChangeNotifier;
        private readonly ISimplifiedFindingsChangedNotifier _findingsChangedNotifier;
        private readonly ISimplifiedEntityChangedPublisher _entityChangedPublisher;
        private readonly ISimplifiedDomainStatusPublisher _domainStatusPublisher;

        public SimplifiedTlsEntity(
            ITlsEntityConfig tlsEntityConfig,
            IMessageDispatcher dispatcher,
            ILogger<SimplifiedTlsEntity> log,
            ISimplifiedTlsEntityDao simplifiedTlsEntityDao,
            ISimplifiedAdvisoryChangedNotifier<TlsFactory> tlsChangeNotifier,
            ISimplifiedAdvisoryChangedNotifier<CertFactory> certChangeNotifier,
            ISimplifiedFindingsChangedNotifier findingsChangedNotifier,
            ISimplifiedEntityChangedPublisher entityChangedPublisher,
            ISimplifiedDomainStatusPublisher domainStatusPublisher)
        {
            _log = log;
            _tlsEntityConfig = tlsEntityConfig;
            _dispatcher = dispatcher;
            _simplifiedTlsEntityDao = simplifiedTlsEntityDao;
            _tlsChangeNotifier = tlsChangeNotifier;
            _certChangeNotifier = certChangeNotifier;
            _findingsChangedNotifier = findingsChangedNotifier;
            _entityChangedPublisher = entityChangedPublisher;
            _domainStatusPublisher = domainStatusPublisher;
        }

        public Task Handle(SimplifiedTlsScheduledReminder message)
        {
            string ipAddress = message.ResourceId.ToLower();
            _dispatcher.Dispatch(new SimplifiedTlsTestPending(ipAddress), _tlsEntityConfig.SnsTopicArn);
            _log.LogInformation($"A SimplfiedTlsTestPending message for ipAddress: {ipAddress} has been dispatched to SnsTopic: {_tlsEntityConfig.SnsTopicArn}");

            return Task.CompletedTask;
        }

        public Task Handle(SimplifiedTlsExpired message)
        {
            string ipAddress = message.ResourceId.ToLower();
            _log.LogInformation($"SimplifiedTlsExpired for ip: {ipAddress}");

            _dispatcher.Dispatch(new SimplifiedTlsTestPending(ipAddress), _tlsEntityConfig.SnsTopicArn);
            _log.LogInformation($"A SimplfiedTlsTestPending message for ipAddress: {ipAddress} has been dispatched to SnsTopic: {_tlsEntityConfig.SnsTopicArn}");

            return Task.CompletedTask;
        }

        public class EntityPair
        {
            public SimplifiedTlsEntityState HostEntity { get; set; }
            public SimplifiedTlsEntityState IpEntity { get; set; }
        }

        private async Task<(SimplifiedTlsEntityState globalState, Dictionary<string, List<EntityPair>>)> GetEntityPairs(string ipAddress)
        {
            var (count, entities) = await _simplifiedTlsEntityDao.FindRelatedEntitiesByIp(ipAddress);

            if (count == 0)
            {
                _log.LogInformation($"No entities for hosts associated with IP {ipAddress}");
                return (null, null);
            }

            _log.LogInformation($"Retrieved {count} entities for hosts associated with IP {ipAddress}");

            var deserialisedEntities = entities.ToList();

            var globalEntities = deserialisedEntities.Where(x => x.Hostname == "*").ToDictionary(x => x.IpAddress);

            globalEntities.TryGetValue(ipAddress, out SimplifiedTlsEntityState globalEntity);

            globalEntity ??= new SimplifiedTlsEntityState
            {
                IpAddress = ipAddress,
                Hostname = "*",
            };

            Dictionary<string, List<EntityPair>> entitiesByHostname = deserialisedEntities
                .Where(x => x.Hostname != "*")
                .GroupBy(x => x.Hostname)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Select(x =>
                    {
                        globalEntities.TryGetValue(x.IpAddress, out SimplifiedTlsEntityState ipEntity);
                        return new EntityPair { HostEntity = x, IpEntity = ipEntity };
                    }).ToList(),
                    StringComparer.InvariantCultureIgnoreCase);

            return (globalEntity, entitiesByHostname);
        }

        public async Task Handle(SimplifiedTlsTestResults message)
        {
            string ipAddress = message.Id;

            using (_log.BeginScope(new Dictionary<string, string>
            {
                ["IpAddress"] = ipAddress
            }))
            {
                message.AdvisoryMessages ??= new List<NamedAdvisory>();
                _log.LogInformation($"A SimplifiedTlsTestResults message received containing {message.AdvisoryMessages?.Count ?? 0} advisories, {message.SimplifiedTlsConnectionResults?.Count ?? 0} results and {message.Certificates?.Count ?? 0} certificates");

                var (globalEntity, entitiesByHostname) = await GetEntityPairs(ipAddress);

                if (entitiesByHostname == null) return;

                var hostnames = entitiesByHostname.Keys.ToList();

                SimplifiedHostCertificateResult certificatesResult = new SimplifiedHostCertificateResult(ipAddress)
                {
                    Hostnames = hostnames,
                    SimplifiedTlsConnectionResults = message.SimplifiedTlsConnectionResults,
                    Certificates = message.Certificates
                };

                _dispatcher.Dispatch(certificatesResult, _tlsEntityConfig.SnsTopicArn);
                _log.LogInformation($"Dispatched a SimplifiedHostCertificatesResult message to SnsTopic {_tlsEntityConfig.SnsTopicArn}");

                var globalEntityWithChanges = ShallowClone(globalEntity);
                globalEntityWithChanges.TlsAdvisories = message.AdvisoryMessages;
                globalEntityWithChanges.TlsLastUpdated = message.Timestamp;
                globalEntityWithChanges.SimplifiedTlsConnectionResults = message.SimplifiedTlsConnectionResults;

                await _simplifiedTlsEntityDao.SaveState(globalEntityWithChanges);
                _log.LogInformation($"SimplifiedTlsTestResults for ip: {ipAddress} - last updated: {message.Timestamp} - advisory count: {message.AdvisoryMessages.Count}");

                var domainsByHostname = await _simplifiedTlsEntityDao.GetDomainsByHostnameForIp(ipAddress);

                foreach (var kvp in entitiesByHostname)
                {
                    string hostname = kvp.Key;

                    if (hostname == "*") continue;

                    using (_log.BeginScope(new Dictionary<string, string>
                    {
                        ["Hostname"] = hostname,
                    }))
                    {
                        var distinctOldAdvisories = kvp.Value
                            .Where(x => x.IpEntity?.TlsAdvisories != null)
                            .SelectMany(x => x.IpEntity.TlsAdvisories)
                            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                            .DistinctBy(x => x.Name)
                            .ToList();

                        var distinctNewAdvisories = kvp.Value
                            .Where(x => x.IpEntity?.TlsAdvisories != null && x.HostEntity.IpAddress != ipAddress)
                            .SelectMany(x => x.IpEntity.TlsAdvisories)
                            .Concat(message.AdvisoryMessages)
                            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                            .DistinctBy(x => x.Name)
                            .ToList();

                        var oldHostCertAdvisories = kvp.Value
                            .Where(x => x.HostEntity?.CertAdvisories != null)
                            .SelectMany(x => x.HostEntity.CertAdvisories);
                        var oldIpCertAdvisories = kvp.Value
                            .Where(x => x.IpEntity?.CertAdvisories != null)
                            .SelectMany(x => x.IpEntity.CertAdvisories);
                        var distinctOldCertAdvisories = oldHostCertAdvisories
                            .Concat(oldIpCertAdvisories)
                            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                            .DistinctBy(x => x.Name)
                            .ToList();

                        SimplifiedEmailSecTlsEntityState hostState = new SimplifiedEmailSecTlsEntityState
                        {
                            Hostname = hostname,
                            TlsLastUpdated = globalEntityWithChanges.TlsLastUpdated,
                            TlsAdvisories = distinctNewAdvisories,
                            CertsLastUpdated = globalEntityWithChanges.CertsLastUpdated,
                            CertAdvisories = distinctOldCertAdvisories,
                        };

                        _entityChangedPublisher.Publish(hostname, hostState, nameof(SimplifiedTlsTestResults));
                        _log.LogDebug($"Published EntityChanged for hostname {hostname}");

                        if (domainsByHostname.TryGetValue(hostname, out List<string> domains))
                        {
                            _tlsChangeNotifier.Notify(hostname, domains, distinctOldAdvisories, distinctNewAdvisories);
                            _findingsChangedNotifier.Handle(hostname, domains, "tls", distinctOldAdvisories, distinctNewAdvisories);
                            _log.LogDebug($"Triggered change notification for {domains.Count} domains");
                        }
                        else
                        {
                            _log.LogDebug("No domains for hostname");
                        }
                    }
                }
            }
        }

        public void AppendRootCertificateIfPresent(string rootCertificateThumbprint, SimplifiedTlsEntityState state)
        {
            if (String.IsNullOrEmpty(rootCertificateThumbprint))
            {
                _log.LogWarning("No root certificate identified. Certificate Thumbprint array may not include root certificate!");
            }
            else
            {
                // append the root certificate to each thumbprint array
                _log.LogInformation("Found root certificate, adding to thumbprint array");
                state.SimplifiedTlsConnectionResults.ForEach(res =>
                {
                    if (res.CertificateThumbprints.Length > 0 && !res.CertificateThumbprints.Any(t => t == rootCertificateThumbprint))
                    {
                        _log.LogInformation($"Added root cert to thumbprint array with certificate count ${res.CertificateThumbprints.Count()}");
                        // ambigous use of Append with Linq and MoreLinq
                        res.CertificateThumbprints = Enumerable.Append(res.CertificateThumbprints, rootCertificateThumbprint).ToArray();
                    }
                });
            }
        }

        public async Task Handle(SimplifiedHostCertificateEvaluated message)
        {
            string ipAddress = message.Id;
            
            using (_log.BeginScope(new Dictionary<string, string>
            {
                ["IpAddress"] = ipAddress
            }))
            {
                var newGlobalCertificateAdvisories = message.CertificateAdvisoryMessages ?? new List<NamedAdvisory>();
                var hostSpecificCertificateAdvisoryMessages = message.HostSpecificCertificateAdvisoryMessages ?? new Dictionary<string, List<NamedAdvisory>>();

                _log.LogInformation($"A SimplifiedHostCertificateEvaluated message received containing {newGlobalCertificateAdvisories.Count} advisories");

                var (globalEntity, entitiesByHostname) = await GetEntityPairs(ipAddress);

                if (globalEntity == null) return;

                SimplifiedTlsEntityState globalEntityWithChanges = ShallowClone(globalEntity);
                globalEntityWithChanges.CertAdvisories = message.CertificateAdvisoryMessages;
                globalEntityWithChanges.CertsLastUpdated = message.Timestamp;
                globalEntityWithChanges.Certificates = message.Certificates;

                AppendRootCertificateIfPresent(message.RootCertificateThumbprint, globalEntityWithChanges);

                await _simplifiedTlsEntityDao.SaveState(globalEntityWithChanges);
                _log.LogInformation($"Saved {message.CertificateAdvisoryMessages.Count} global cert advisories changes");

                var domainsByHostname = await _simplifiedTlsEntityDao.GetDomainsByHostnameForIp(ipAddress);

                foreach (var hostname in message.Hostnames)
                {
                    using (_log.BeginScope(new Dictionary<string, string>
                    {
                        ["Hostname"] = hostname
                    }))
                    {
                        if (!entitiesByHostname.TryGetValue(hostname, out List<EntityPair> entityPairsForHost))
                        {
                            _log.LogDebug($"No entity found for host {hostname} and IP {ipAddress}");
                            continue;
                        }

                        var entityPair = entityPairsForHost.FirstOrDefault(x => x.HostEntity.IpAddress == ipAddress);
                        if (entityPair == null) continue;

                        var entityOld = entityPair.HostEntity;

                        _log.LogDebug($"Found entity for host {hostname} and IP {ipAddress}");

                        var oldHostCertificateAdvisories = entityOld.CertAdvisories ?? new List<NamedAdvisory>();

                        hostSpecificCertificateAdvisoryMessages.TryGetValue(hostname, out List<NamedAdvisory> newHostCertificateAdvisories);
                        newHostCertificateAdvisories = newHostCertificateAdvisories ?? new List<NamedAdvisory>();

                        if (newHostCertificateAdvisories.Count > 0 || oldHostCertificateAdvisories.Count != newHostCertificateAdvisories.Count)
                        {
                            var hostEntityWithChanges = ShallowClone(entityOld);
                            hostEntityWithChanges.CertAdvisories = newHostCertificateAdvisories;
                            hostEntityWithChanges.CertsLastUpdated = message.Timestamp;

                            await _simplifiedTlsEntityDao.SaveState(hostEntityWithChanges);
                            _log.LogInformation($"Saved {newHostCertificateAdvisories.Count} additional advisories for hostname {hostname}");
                        }

                        var oldHostAdvisories = entityPairsForHost
                            .Where(x => x.HostEntity.CertAdvisories != null)
                            .SelectMany(x => x.HostEntity.CertAdvisories);
                        var oldIpAdvisories = entityPairsForHost
                            .Where(x => x.IpEntity?.CertAdvisories != null)
                            .SelectMany(x => x.IpEntity.CertAdvisories);
                        var distinctOldAdvisories = oldHostAdvisories
                            .Concat(oldIpAdvisories)
                            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                            .DistinctBy(x => x.Name)
                            .ToList();

                        var entitiesExcludingCurrentIp = entityPairsForHost.Where(x => x.HostEntity.IpAddress != ipAddress).ToList();
                        var newHostAdvisories = entitiesExcludingCurrentIp
                            .Where(x => x.HostEntity.CertAdvisories != null)
                            .SelectMany(x => x.HostEntity.CertAdvisories);
                        var newIpAdvisories = entitiesExcludingCurrentIp
                            .Where(x => x.IpEntity?.CertAdvisories != null)
                            .SelectMany(x => x.IpEntity.CertAdvisories);
                        var distinctNewAdvisories = newHostAdvisories
                            .Concat(newIpAdvisories)
                            .Concat(newHostCertificateAdvisories)
                            .Concat(newGlobalCertificateAdvisories)
                            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                            .DistinctBy(x => x.Name)
                            .ToList();

                        var oldTlsAdvisories = entityPairsForHost
                            .Where(x => x.IpEntity?.TlsAdvisories != null)
                            .SelectMany(x => x.IpEntity.TlsAdvisories)
                            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                            .DistinctBy(x => x.Name)
                            .ToList();

                        SimplifiedEmailSecTlsEntityState hostState = new SimplifiedEmailSecTlsEntityState
                        {
                            Hostname = hostname,
                            TlsLastUpdated = globalEntityWithChanges.TlsLastUpdated,
                            TlsAdvisories = oldTlsAdvisories,
                            CertsLastUpdated = globalEntityWithChanges.CertsLastUpdated,
                            CertAdvisories = distinctNewAdvisories,
                        };

                        _entityChangedPublisher.Publish(hostname, hostState, nameof(SimplifiedHostCertificateEvaluated));
                        _log.LogDebug($"Published EntityChanged for hostname {hostname}");

                        if (domainsByHostname.TryGetValue(hostname, out List<string> domains))
                        {
                            _certChangeNotifier.Notify(hostname, domains, distinctOldAdvisories, distinctNewAdvisories);
                            _findingsChangedNotifier.Handle(hostname, domains, "tls-certificates", distinctOldAdvisories, distinctNewAdvisories);
                            _log.LogDebug($"Triggered change notification for {domains.Count} domains");
                        }
                        else
                        {
                            _log.LogDebug("No domains for hostname");
                        }
                    }
                }

                await _domainStatusPublisher.CalculateAndPublishDomainStatuses(ipAddress);

                ReminderSuccessful reminderSuccessful = new ReminderSuccessful(
                    Guid.NewGuid().ToString(),
                    ServiceName,
                    ipAddress,
                    message.Timestamp
                    );

                _dispatcher.Dispatch(reminderSuccessful, _tlsEntityConfig.SnsTopicArn);
                _log.LogInformation($"A ReminderSuccessful message for IP: {ipAddress} has been dispatched to SnsTopic: {_tlsEntityConfig.SnsTopicArn}");
            }
        }

        private SimplifiedTlsEntityState ShallowClone(SimplifiedTlsEntityState source)
        {
            return source == null
                ? null
                : new SimplifiedTlsEntityState
                {
                    IpAddress = source.IpAddress,
                    Hostname = source.Hostname,
                    TlsAdvisories = source.TlsAdvisories,
                    TlsLastUpdated = source.TlsLastUpdated,
                    Certificates = source.Certificates,
                    CertAdvisories = source.CertAdvisories,
                    CertsLastUpdated = source.CertsLastUpdated,
                    SimplifiedTlsConnectionResults = source.SimplifiedTlsConnectionResults
                };
        }
    }
}
