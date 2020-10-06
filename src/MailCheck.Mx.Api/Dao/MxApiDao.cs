using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using MailCheck.Common.Data.Abstractions;
using MailCheck.Common.Data.Util;
using MailCheck.Mx.Contracts.Entity;
using MailCheck.Mx.Contracts.Poller;
using MailCheck.Mx.Contracts.SharedDomain;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using MySqlHelper = MailCheck.Common.Data.Util.MySqlHelper;

namespace MailCheck.Mx.Api.Dao
{
    public interface IMxApiDao
    {
        Task<MxEntityState> GetMxEntityState(string domainId);
        Task<List<MxEntityState>> GetMxEntityStates(List<string> domains);
        Task<Dictionary<string, TlsEntityState>> GetTlsEntityStates(List<string> hostnames);
    }

    public class MxApiDao : IMxApiDao
    {
        private readonly IConnectionInfoAsync _connectionInfo;
        private readonly ILogger<MxApiDao> _log;

        public MxApiDao(IConnectionInfoAsync connectionInfo, ILogger<MxApiDao> log)
        {
            _connectionInfo = connectionInfo;
            _log = log;
        }

        public async Task<MxEntityState> GetMxEntityState(string id)
        {
            List<MxEntityState> result = await GetMxEntityStates(new List<string> {id});
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
                                MxState = (MxState) reader.GetInt32("mxState"),
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

        public async Task<Dictionary<string, TlsEntityState>> GetTlsEntityStates(List<string> hostnames)
        {
            if (hostnames == null || hostnames.Count == 0)
            {
                return null;
            }

            string connectionString = await _connectionInfo.GetConnectionStringAsync();
            Dictionary<string, TlsEntityState> results = new Dictionary<string, TlsEntityState>();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                string query = string.Format(MxApiDaoResources.GetTlsEntityStates,
                    string.Join(',', hostnames.Select((_, i) => $"@hostname{i}")));

                MySqlParameter[] parameters = hostnames
                    .Select((hostname, i) => new MySqlParameter($"hostname{i}", ReverseUrl(hostname)))
                    .ToArray();

                await connection.OpenAsync();

                using (DbDataReader reader = await MySqlHelper.ExecuteReaderAsync(connection, query, parameters))
                {
                    while (await reader.ReadAsync())
                    {
                        string hostname = ReverseUrl(reader.GetString("hostname"));
                        if (!results.ContainsKey(hostname))
                        {
                            results.Add(hostname,
                                JsonConvert.DeserializeObject<TlsEntityState>(reader.GetString("state")));
                        }
                    }
                }

                connection.Close();
            }

            return results;
        }

        private string ReverseUrl(string url)
        {
            return string.Join(".", url.Split('.').Reverse()).ToLower();
        }
    }
}