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

namespace MailCheck.Mx.TlsEntity.Dao
{
    public interface ITlsEntityDao
    {
        Task<TlsEntityState> Get(string host);
        Task Save(TlsEntityState state);
        Task Delete(string host);
        Task<Dictionary<string, List<TlsEntityState>>> GetDomains(string hostname);
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

        public async Task<Dictionary<string, List<TlsEntityState>>> GetDomains(string hostname)
        {
            string connectionString = await _connectionInfo.GetConnectionStringAsync();

            Dictionary<string, List<TlsEntityState>> results = new Dictionary<string, List<TlsEntityState>>();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (DbDataReader reader = await MySqlHelper.ExecuteReaderAsync(connection,
                    TlsEntityDaoResources.GetDomainMxHosts, new MySqlParameter("hostname", ReverseUrl(hostname))))
                {
                    while (await reader.ReadAsync())
                    {
                        string domain = ReverseUrl(reader.GetString("domain"));
                        TlsEntityState state = JsonConvert.DeserializeObject<TlsEntityState>(reader.GetString("state"));
                        if (results.ContainsKey(domain))
                        {
                            results[domain].Add(state);
                        }
                        else
                        {
                            results[domain] = new List<TlsEntityState> {state};
                        }
                    }
                }

                connection.Close();
            }

            return results;
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

        private string ReverseUrl(string url)
        {
            return string.Join(".", url.Split('.').Reverse()).ToLower();
        }
    }
}