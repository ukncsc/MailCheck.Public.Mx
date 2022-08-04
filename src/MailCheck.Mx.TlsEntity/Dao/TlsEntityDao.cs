using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using MailCheck.Common.Data.Abstractions;
using MailCheck.Mx.Contracts.Entity;
using MySql.Data.MySqlClient;
using MailCheck.Common.Data.Util;
using Newtonsoft.Json;
using MySqlHelper = MailCheck.Common.Data.Util.MySqlHelper;
using MailCheck.Mx.Contracts.SharedDomain;

namespace MailCheck.Mx.TlsEntity.Dao
{
    public interface ITlsEntityDao
    {
        Task<TlsEntityState> Get(string host);
        Task Save(TlsEntityState state);
        Task Delete(string host);
        Task<Dictionary<string, List<HostErrors>>> GetRelatedDomainsWithErrors(string hostname);
        Task<List<string>> GetDomainsFromHost(string hostname);
    }

    public class TlsEntityDao : ITlsEntityDao
    {
        private readonly IConnectionInfoAsync _connectionInfo;

        public TlsEntityDao(IConnectionInfoAsync connectionInfo)
        {
            _connectionInfo = connectionInfo;
        }

        public async Task Save(TlsEntityState state)
        {
            string connectionString = await _connectionInfo.GetConnectionStringAsync();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                
                await MySqlHelper.ExecuteScalarAsync(connection,
                    TlsEntityDaoResources.SaveTlsEntity,
                    new MySqlParameter("hostname", ReverseUrl(state.Id)),
                    new MySqlParameter("state", JsonConvert.SerializeObject(state)));

                connection.Close();
            }
        }

        public async Task<TlsEntityState> Get(string hostname)
        {
            string connectionString = await _connectionInfo.GetConnectionStringAsync();
            TlsEntityState result = null;

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (DbDataReader reader = await MySqlHelper.ExecuteReaderAsync(connection,
                    TlsEntityDaoResources.GetTlsEntity, new MySqlParameter("hostname", ReverseUrl(hostname))))
                {
                    while (await reader.ReadAsync())
                    {
                        result = JsonConvert.DeserializeObject<TlsEntityState>(reader.GetString("state"));
                    }
                }

                connection.Close();
            }

            return result;
        }

        public async Task Delete(string hostname)
        {
            string connectionString = await _connectionInfo.GetConnectionStringAsync();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                await MySqlHelper.ExecuteNonQueryAsync(connection,
                    TlsEntityDaoResources.DeleteTlsEntity,
                    new MySqlParameter("hostname", ReverseUrl(hostname)));

                connection.Close();
            }
        }

        public async Task<Dictionary<string, List<HostErrors>>> GetRelatedDomainsWithErrors(string hostname)
        {
            Dictionary<string, List<HostErrors>> domainHostErrors = new Dictionary<string, List<HostErrors>>();

            string connectionString = await _connectionInfo.GetConnectionStringAsync();

            Dictionary<string, List<string>> domainHosts = new Dictionary<string, List<string>>();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (DbDataReader reader = await MySqlHelper.ExecuteReaderAsync(connection,
                    TlsEntityDaoResources.GetDomainMxHosts, new MySqlParameter("hostname", ReverseUrl(hostname))))
                {
                    while (await reader.ReadAsync())
                    {
                        string domain = ReverseUrl(reader.GetString("domain"));
                        string reverseHost = reader.GetString("hostname");
                        if (domainHosts.TryGetValue(domain, out List<string> associatedHosts))
                        {
                            associatedHosts.Add(reverseHost);
                        }
                        else
                        {
                            domainHosts[domain] = new List<string> {reverseHost};
                        }
                    }
                }
                List<string> distinctHostnames = domainHosts.Values.SelectMany(x => x).Distinct().ToList();
                Dictionary<string, HostErrors> hostErrors = await GetErrorsFromHosts(connection, distinctHostnames);
                domainHostErrors = domainHosts.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Select(x => hostErrors[x]).ToList());

                connection.Close();
            }

            return domainHostErrors;
        }

        public async Task<List<string>> GetDomainsFromHost(string hostname)
        {
            string connectionString = await _connectionInfo.GetConnectionStringAsync();

            List<string> results = new List<string>();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (DbDataReader reader = await MySqlHelper.ExecuteReaderAsync(connection,
                    TlsEntityDaoResources.GetDomainsFromMxHost, new MySqlParameter("hostname", ReverseUrl(hostname))))
                {
                    while (await reader.ReadAsync())
                    {
                        string domain = ReverseUrl(reader.GetString("domain"));
                        results.Add(domain);
                    }
                }

                connection.Close();
            }

            return results;
        }

        private async Task<Dictionary<string, HostErrors>> GetErrorsFromHosts(MySqlConnection connection, List<string> hostnames)
        {
            Dictionary<string, HostErrors> results = hostnames.ToDictionary(host => host, _ => new HostErrors());

            string hostnamesString = null;
            MySqlParameter[] hostParams = null;

            if (hostnames != null && hostnames.Count > 0)
            {
                hostnamesString = string.Join(",", hostnames.Select((_, i) => $"@host{i}"));

                hostParams = hostnames.Select((x, i) => new MySqlParameter($"@host{i}", x)).ToArray();
            }
            else
            {
                return results;
            }

            string commandText = string.Format(TlsEntityDaoResources.GetHostsStates, hostnamesString);

            using (DbDataReader reader = await MySqlHelper.ExecuteReaderAsync(connection,
                commandText, hostParams))
            {
                while (await reader.ReadAsync())
                {
                    string hostname = reader.GetString("hostname");
                    string certErrorString = reader.GetString("certErrors");
                    string configErrorString = reader.GetString("configErrors");

                    results[hostname].CertErrors = certErrorString == null ? Array.Empty<Error>() : JsonConvert.DeserializeObject<Error[]>(certErrorString);
                    results[hostname].ConfigErrors = configErrorString == null ? Array.Empty<EvaluatorResult?>() : JsonConvert.DeserializeObject<EvaluatorResult?[]>(configErrorString);
                }
            }

            return results;
        }

        private string ReverseUrl(string url)
        {
            return string.Join(".", url.Split('.').Reverse()).ToLower();
        }
    }
}