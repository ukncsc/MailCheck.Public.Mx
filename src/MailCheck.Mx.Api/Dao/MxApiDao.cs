using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MailCheck.Common.Data;
using MailCheck.Common.Data.Abstractions;
using MailCheck.Common.Data.Util;
using MailCheck.Mx.Contracts.Entity;
using MailCheck.Mx.Contracts.Poller;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Simplified;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MySqlHelper = MailCheck.Common.Data.Util.MySqlHelper;

namespace MailCheck.Mx.Api.Dao
{
    public interface IMxApiDao
    {
        Task<MxEntityState> GetMxEntityState(string domainId);
        Task<List<MxEntityState>> GetMxEntityStates(List<string> domains);
        Task<List<SimplifiedTlsEntityState>> GetSimplifiedStates(string domainId);
        Task<Dictionary<string, int>> GetPreferences(string domainId);
    }

    public class MxApiDao : IMxApiDao
    {
        private readonly IDatabase _database;
        private readonly IConnectionInfoAsync _connectionInfo;
        private readonly ILogger<MxApiDao> _log;

        public MxApiDao(IConnectionInfoAsync connectionInfo, ILogger<MxApiDao> log, IDatabase database)
        {
            _connectionInfo = connectionInfo;
            _log = log;
            _database = database;
        }

        public async Task<MxEntityState> GetMxEntityState(string id)
        {
            List<MxEntityState> result = await GetMxEntityStates(new List<string> { id });
            return result.FirstOrDefault();
        }

        public async Task<List<MxEntityState>> GetMxEntityStates(List<string> domains)
        {
            string connectionString = await _connectionInfo.GetConnectionStringAsync();

            List<(string, HostMxRecord)> hostMxRecords = new List<(string, HostMxRecord)>();
            Dictionary<string, MxEntityState> mxEntityStates = new Dictionary<string, MxEntityState>();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                string query = string.Format(MxApiDaoResources.GetMxRecords,
                    string.Join(',', domains.Select((_, i) => $"@domain{i}")));

                MySqlParameter[] parameters = domains
                    .Select((domain, i) => new MySqlParameter($"domain{i}", ReverseUrl(domain)))
                    .ToArray();

                await connection.OpenAsync();

                using (DbDataReader reader = await MySqlHelper.ExecuteReaderAsync(connection, query, parameters))
                {
                    while (await reader.ReadAsync())
                    {
                        string domain = ReverseUrl(reader.GetString("domain"));
                        if (!mxEntityStates.ContainsKey(domain))
                        {
                            mxEntityStates.Add(domain, new MxEntityState(domain)
                            {
                                MxState = (MxState)reader.GetInt32("mxState"),
                                LastUpdated = reader.GetDateTime("lastUpdated"),
                                Error = JsonConvert.DeserializeObject<Message>(reader.GetString("error")),
                                HostMxRecords = new List<HostMxRecord>()
                            });
                        }

                        hostMxRecords.Add((domain,
                            JsonConvert.DeserializeObject<HostMxRecord>(reader.GetString("hostMxRecord").ToLower())));
                    }
                }

                connection.Close();
            }

            foreach ((string, HostMxRecord) tuple in hostMxRecords)
            {
                mxEntityStates[tuple.Item1].HostMxRecords.Add(tuple.Item2);
            }

            return mxEntityStates.Values.ToList();
        }

        public async Task<List<SimplifiedTlsEntityState>> GetSimplifiedStates(string domainId)
        {
            IEnumerable<SimplifiedTlsEntityStateContainer> items;

            using (DbConnection connection = await _database.CreateAndOpenConnectionAsync())
            {
                items = await connection.QueryAsync<SimplifiedTlsEntityStateContainer>(
                    MxApiDaoResources.GetSimplifiedTlsEntityStates, new { domainId = ReverseUrl(domainId) });

            }
            return UnwrapEntities(items)
                .Where(x => x != null)
                .ToList();
        }

        public async Task<Dictionary<string, int>> GetPreferences(string domainId)
        {
            using (DbConnection connection = await _database.CreateAndOpenConnectionAsync())
            {
                return (await connection.QueryAsync<(string, int)>(MxApiDaoResources.GetPreference, new { domainId = ReverseUrl(domainId) }))
                    .ToDictionary(x => ReverseUrl(x.Item1), x => x.Item2);
            }
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

        internal static SimplifiedTlsEntityState UnwrapEntity(SimplifiedTlsEntityStateContainer container)
        {
            if (string.IsNullOrWhiteSpace(container.HostJson) && string.IsNullOrWhiteSpace(container.IpJson))
            {
                return null;
            }

            var hostDoc = string.IsNullOrWhiteSpace(container.HostJson) ? new JObject() : JObject.Parse(container.HostJson);
            var ipDoc = string.IsNullOrWhiteSpace(container.IpJson) ? new JObject() : JObject.Parse(container.IpJson);

            var hostDates = hostDoc.ToObject<DateContainer>();
            var ipDates = ipDoc.ToObject<DateContainer>();

            ipDoc.Merge(hostDoc);

            var state = ipDoc.ToObject<SimplifiedTlsEntityState>();
            state.Hostname = ReverseUrl(container.Hostname);
            state.IpAddress = container.IpAddress;
            state.CertsLastUpdated = LatestDate(hostDates.CertsLastUpdated, ipDates.CertsLastUpdated);
            state.TlsLastUpdated = LatestDate(hostDates.TlsLastUpdated, ipDates.TlsLastUpdated);

            return state;
        }

        private static DateTime? LatestDate(params DateTime?[] dates)
        {
            var maxDate = dates
                .Select(date => date ?? DateTime.MinValue)
                .Max();

            return (maxDate == DateTime.MinValue) ? null : (DateTime?)maxDate;
        }

        internal class SimplifiedTlsEntityStateContainer
        {
            public string Hostname { get; set; }
            public string IpAddress { get; set; }
            public string IpJson { get; set; }
            public string HostJson { get; set; }
        }

        private class DateContainer
        {
            public DateTime? TlsLastUpdated { get; set; }
            public DateTime? CertsLastUpdated { get; set; }
        }
    }
}