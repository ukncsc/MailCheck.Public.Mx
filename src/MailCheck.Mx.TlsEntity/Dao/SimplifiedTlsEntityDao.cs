using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MailCheck.Common.Contracts.Advisories;
using MailCheck.Common.Data;
using MailCheck.DomainStatus.Contracts;
using MailCheck.Mx.Contracts.Simplified;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MailCheck.Mx.TlsEntity.Dao
{
    public interface ISimplifiedTlsEntityDao
    {
        Task<List<string>> SyncIpAddressForHostname(string hostname, List<string> ipAddresses);
        Task SaveState(SimplifiedTlsEntityState state);
        Task<(int, IEnumerable<SimplifiedTlsEntityState>)> FindEntitiesByIp(string ipAddress);
        Task<Dictionary<string, List<string>>> GetDomainsByHostnameForIp(string ipAddress);
        Task<(int, IEnumerable<SimplifiedTlsEntityState>)> FindRelatedEntitiesByIp(string ipAddress);
        Task<Dictionary<string, Status>> GetMaxAdvisoryStatusesForAffectedDomainsByMxHostIp(string ipAddress);
    }

    public class SimplifiedTlsEntityDao: ISimplifiedTlsEntityDao
    {
        private static readonly Status[] SuccessOnly = new[] { Status.Success };
        private readonly IDatabase _database;
        private readonly ILogger<SimplifiedTlsEntityDao> _logger;

        private class SimplifiedTlsEntityStateContainer
        {
            public string Hostname { get; set; }
            public string IpAddress { get; set; }
            public string Json { get; set; }
        }

        internal class AdvisoryStatusContainer
        {
            public string Domain { get; set; }
            public string Hostname { get; set; }
            public string Statuses { get; set; }
        }

        public SimplifiedTlsEntityDao(IDatabase database, ILogger<SimplifiedTlsEntityDao> logger)
        {
            _database = database;
            _logger = logger;
        }

        public async Task<List<string>> SyncIpAddressForHostname(string hostname, List<string> newIpAddresses)
        {
            _logger.LogInformation($"SyncIpAddressForHostname for Host: {hostname} -> ipAddress: {string.Join(", ", newIpAddresses.ToArray())}");

            var reversedHostname = ReverseUrl(hostname);

            using (DbConnection connection = await _database.CreateAndOpenConnectionAsync())
            using (DbTransaction transaction = await connection.BeginTransactionAsync())
            {
                List<string> existingIpAddresses = (await connection.QueryAsync<HostnameIpAddressDto>(
                    SimplifiedTlsEntityDaoResources.GetIpAddressesForHost,
                    new { hostname = reversedHostname },
                    transaction
                ))
                .Select(i => i.IpAddress)
                .ToList();

                _logger.LogInformation($"SyncIpAddressForHostname existingIpAddresses for Host: {hostname} -> ipAddress: {string.Join(", ", existingIpAddresses.ToArray())}");

                List<string> ipsToDelete = existingIpAddresses.Except(newIpAddresses).ToList();
                List<string> ipsToAdd = newIpAddresses.Except(existingIpAddresses).ToList();

                _logger.LogInformation($"SyncIpAddressForHostname ipsToDelete for Host: {hostname} -> ipAddress: {string.Join(", ", ipsToDelete.ToArray())}");
                _logger.LogInformation($"SyncIpAddressForHostname ipsToAdd for Host: {hostname} -> ipAddress: {string.Join(", ", ipsToAdd.ToArray())}");

                object[] deleteQueryParams = ipsToDelete.Select(ip => new { ipAddress = ip, hostname = reversedHostname }).ToArray();
                await connection.ExecuteAsync(
                    SimplifiedTlsEntityDaoResources.DeleteIpAddressHostnameAssociations,
                    deleteQueryParams,
                    transaction
                );

                object[] insertQueryParams = ipsToAdd.Select(ip => new { ipAddress = ip, hostname = reversedHostname }).ToArray();
                await connection.ExecuteAsync(
                    SimplifiedTlsEntityDaoResources.SaveIpAddressesHostnameAssociciation,
                    insertQueryParams,
                    transaction
                );

                transaction.Commit();

                return ipsToAdd;
            }
        }

        public async Task<(int, IEnumerable<SimplifiedTlsEntityState>)> FindEntitiesByIp(string ipAddress)
        {
            SimplifiedTlsEntityStateContainer[] containers = Array.Empty<SimplifiedTlsEntityStateContainer>();

            using (DbConnection connection = await _database.CreateAndOpenConnectionAsync())
            {
                containers = (await connection.QueryAsync<SimplifiedTlsEntityStateContainer>(
                    SimplifiedTlsEntityDaoResources.FindEntitiesByIp,
                    new { ipAddress }
                )).ToArray();
            }

            return (containers.Length, UnwrapEntities(containers));
        }

        public async Task<SimplifiedTlsEntityState> GetState(string ipAddress, string hostname)
        {
            SimplifiedTlsEntityStateContainer state = null;

            using (DbConnection connection = await _database.CreateAndOpenConnectionAsync())
            {
                state = (await connection.QueryAsync<SimplifiedTlsEntityStateContainer>(
                    SimplifiedTlsEntityDaoResources.GetStateForIpAndHost,
                    new { ipAddress, hostname = ReverseUrl(hostname) }
                ))
                .FirstOrDefault();
            }

            if (state == null)
            {
                return null;
            }

            return UnwrapEntity(state);
        }

        public async Task SaveState(SimplifiedTlsEntityState state)
        {
            using (DbConnection connection = await _database.CreateAndOpenConnectionAsync())
            {
                await connection.ExecuteAsync(
                    SimplifiedTlsEntityDaoResources.SaveStateForIpAndHost,
                    new
                    {
                        ipAddress = state.IpAddress,
                        hostname = ReverseUrl(state.Hostname),
                        state = JsonConvert.SerializeObject(state)
                    }
                );
            }
        }

        public async Task<Dictionary<string, List<string>>> GetDomainsByHostnameForIp(string ipAddress)
        {
            using (DbConnection connection = await _database.CreateAndOpenConnectionAsync())
            {
                return (await connection.QueryAsync<(string Hostname, string Domain)>(
                    SimplifiedTlsEntityDaoResources.GetHostnamesWithDomainsForIp,
                    new { ipAddress }
                ))
                .GroupBy(row => row.Hostname, row => ReverseUrl(row.Domain))
                .ToDictionary(grp => ReverseUrl(grp.Key), grp => grp.ToList());
            }
        }

        public async Task<(int, IEnumerable<SimplifiedTlsEntityState>)> FindRelatedEntitiesByIp(string ipAddress)
        {
            SimplifiedTlsEntityStateContainer[] containers = Array.Empty<SimplifiedTlsEntityStateContainer>();

            using (DbConnection connection = await _database.CreateAndOpenConnectionAsync())
            {
                containers = (await connection.QueryAsync<SimplifiedTlsEntityStateContainer>(
                    SimplifiedTlsEntityDaoResources.FindRelatedEntitiesByIp,
                    new { ipAddress }
                )).ToArray();
            }

            return (containers.Length, UnwrapEntities(containers));
        }

        public async Task<Dictionary<string, Status>> GetMaxAdvisoryStatusesForAffectedDomainsByMxHostIp(string ipAddress)
        {
            AdvisoryStatusContainer[] containers = Array.Empty<AdvisoryStatusContainer>();

            using (DbConnection connection = await _database.CreateAndOpenConnectionAsync())
            {
                containers = (await connection.QueryAsync<AdvisoryStatusContainer>(
                    SimplifiedTlsEntityDaoResources.GetAdvisoryStatusesForAffectedDomainsByMxHostIp,
                    new { ipAddress }
                )).ToArray();
            }

            return CalculateStatuses(containers);
        }

        internal static Dictionary<string, Status> CalculateStatuses(AdvisoryStatusContainer[] containers)
        {
            return containers
                .Where(c => c.Statuses != null)
                .Select(c => new DomainAdvisories { Domain = c.Domain, Statuses = JsonConvert.DeserializeObject<MessageType[]>(c.Statuses) })
                .GroupBy(d => d.Domain, d => d.Statuses)
                .ToDictionary(g => ReverseUrl(g.Key), g =>
                {
                    return g
                        .SelectMany(s => s)
                        .Select(MapStatus)
                        .Concat(SuccessOnly)
                        .Max();
                });
        }


        private static Status MapStatus(MessageType status)
        {
            return status switch
            {
                MessageType.info => Status.Info,
                MessageType.warning => Status.Warning,
                MessageType.error => Status.Error,
                MessageType.success => Status.Success,
                _ => Status.Success,
            };
        }

        private static string ReverseUrl(string url)
        {
            return string.Join(".", url.Split('.').Reverse()).ToLower();
        }

        private static IEnumerable<SimplifiedTlsEntityState> UnwrapEntities(IEnumerable<SimplifiedTlsEntityStateContainer> containers)
        {
            foreach (var container in containers)
            {
                yield return UnwrapEntity(container);
            }
        }

        private static SimplifiedTlsEntityState UnwrapEntity(SimplifiedTlsEntityStateContainer container)
        {
            if (string.IsNullOrWhiteSpace(container.Json))
            {
                return new SimplifiedTlsEntityState(ReverseUrl(container.Hostname), container.IpAddress);
            }

            var state = JsonConvert.DeserializeObject<SimplifiedTlsEntityState>(container.Json);
            state.Hostname = ReverseUrl(container.Hostname);
            state.IpAddress = container.IpAddress;
            return state;
        }

    }

    public class DomainAdvisories 
    {
        public string Domain { get; set; }

        public MessageType[] Statuses { get; set; }
    }
}
