using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using MailCheck.Common.Data.Abstractions;
using MailCheck.Mx.Contracts.Entity;
using MailCheck.Mx.Contracts.Poller;
using MailCheck.Mx.Contracts.SharedDomain;
using MySqlHelper = MailCheck.Common.Data.Util.MySqlHelper;
using MailCheck.Common.Data;
using System.Text;
using CommonDataUtil = MailCheck.Common.Data.Util.DbDataReaderExtensionMethods;

namespace MailCheck.Mx.Entity.Dao
{
    public class MxEntityDao : IMxEntityDao
    {
        private static readonly Fieldset MxHostFields = new Fieldset { 
            "hostname",
            "hostMxRecord",
            "lastUpdated" 
        };

        private static readonly Fieldset MxRecordFields = new Fieldset {
            "domain",
            "hostname",
            "preference"
        };

        private readonly IConnectionInfoAsync _connectionInfo;
        private readonly ILogger<MxEntityDao> _log;
        private readonly Func<string, IDictionary<string, object>, Task<int>> _saveOperation;

        public MxEntityDao(IConnectionInfoAsync connectionInfo, ILogger<MxEntityDao> log) : this(connectionInfo, log, null) { }

        internal MxEntityDao(
            IConnectionInfoAsync connectionInfo,
            ILogger<MxEntityDao> log,
            Func<string, IDictionary<string, object>, Task<int>> saveOperation
            )
        {
            _connectionInfo = connectionInfo;
            _log = log;
            _saveOperation = saveOperation ?? DefaultSaveToDatabase;
        }

        public async Task Save(MxEntityState mxEntityState)
        {
            string domain = ReverseUrl(mxEntityState.Id);

            var parameters = new Dictionary<string, object>();
            parameters.Add("domain", domain);
            parameters.Add("mxState", mxEntityState.MxState);
            parameters.Add("error", JsonConvert.SerializeObject(mxEntityState.Error));
            parameters.Add("lastUpdated", mxEntityState.LastUpdated);

            var builder = new SqlBuilder();
            string updateMxTables = String.Empty;
            if (mxEntityState.HostMxRecords != null)
            {
                updateMxTables = MxStateDaoResources.DeleteMxRecord;

                if (mxEntityState.HostMxRecords.Count > 0)
                {
                    updateMxTables += MxStateDaoResources.UpsertMxHost + MxStateDaoResources.UpsertMxRecord;

                    builder.SetToken("MxHostValues", MxHostFields.ToValuesParameterListSql(mxEntityState.HostMxRecords.Count));
                    builder.SetToken("MxRecordValues", MxRecordFields.ToValuesParameterListSql(mxEntityState.HostMxRecords.Count));

                    mxEntityState.HostMxRecords.Select((hostMxRecord, index) =>
                    {
                        string host = ReverseUrl(hostMxRecord.Id);

                        parameters.Add($"domain_{index}", domain);
                        parameters.Add($"hostname_{index}", host);
                        parameters.Add($"hostMxRecord_{index}", JsonConvert.SerializeObject(hostMxRecord));
                        parameters.Add($"lastUpdated_{index}", mxEntityState.LastUpdated);
                        parameters.Add($"preference_{index}", hostMxRecord.Preference);

                        return index;
                    }).ToArray();
                }
            }

            var commandText = builder.Build($"{MxStateDaoResources.UpsertDomain}{updateMxTables}");
            await _saveOperation(commandText, parameters);
        }

        public async Task UpdateState(string domain, MxState state)
        {
            var parameters = new Dictionary<string, object>
            {
                ["domain"] = ReverseUrl(domain),
                ["mxState"] = state,
                ["error"] = null,
                ["lastUpdated"] = null
            };

            await _saveOperation(MxStateDaoResources.UpsertDomain, parameters);
        }

        public async Task<MxEntityState> Get(string domain)
        {
            MxEntityState result = null;
            
            using (var connection = await CreateAndOpenConnection())
            using (DbDataReader reader = await MySqlHelper.ExecuteReaderAsync(connection,
                    MxStateDaoResources.GetMxRecord, new MySqlParameter("domain", ReverseUrl(domain))))
            {
                while (await reader.ReadAsync())
                {
                    result = result ?? new MxEntityState(domain)
                    {
                        Error = JsonConvert.DeserializeObject<Message>(CommonDataUtil.GetString(reader, "error")),
                        LastUpdated = CommonDataUtil.GetDateTimeNullable(reader, "lastUpdated"),
                        MxState = (MxState)CommonDataUtil.GetInt32(reader, "mxState"),
                        HostMxRecords = new List<HostMxRecord>()
                    };

                    string hostMxRecord = CommonDataUtil.GetString(reader, "hostMxRecord");
                    if (!string.IsNullOrEmpty(hostMxRecord))
                    {
                        result.HostMxRecords.Add(JsonConvert.DeserializeObject<HostMxRecord>(hostMxRecord));
                    }
                }
            }

            return result;
        }

        public async Task Delete(string domain)
        {
            var parameters = new Dictionary<string, object>
            {
                ["domain"] = ReverseUrl(domain)
            };

            await _saveOperation(MxStateDaoResources.DeleteDomain, parameters);
        }

        public async Task<List<string>> GetHostsUniqueToDomain(string domain)
        {
            List<string> hostnames = new List<string>();
            
            if (string.IsNullOrEmpty(domain))
            {
                return hostnames;
            }

            using (var connection = await CreateAndOpenConnection())
            using (DbDataReader reader = await MySqlHelper.ExecuteReaderAsync(connection,
                    MxStateDaoResources.GetHostsUniqueToDomain, new MySqlParameter("domain", ReverseUrl(domain))))
            {
                while (await reader.ReadAsync())
                {
                    hostnames.Add(ReverseUrl(CommonDataUtil.GetString(reader, "hostname")));
                }
            }

            return hostnames;
        }

        public async Task DeleteHosts(List<string> hostnames)
        {
            if (hostnames == null || hostnames.Count == 0)
            {
                return;
            }

            var parameters = new Dictionary<string, object>();
            StringBuilder stringBuilder = new StringBuilder(MxStateDaoResources.DeleteHosts);
            for (int i = 0; i < hostnames.Count; i++)
            {
                stringBuilder.Append(string.Format(MxStateDaoResources.DeleteHostsValueFormatString, i));
                stringBuilder.Append(i < hostnames.Count - 1 ? "," : ");");

                parameters.Add($"a{i}", ReverseUrl(hostnames[i]));
            }
            await _saveOperation(stringBuilder.ToString(), parameters);
        }

        private string ReverseUrl(string url)
        {
            return string.Join(".", url.Split('.').Reverse()).ToLower();
        }

        private async Task<MySqlConnection> CreateAndOpenConnection()
        {
            string connectionString = await _connectionInfo.GetConnectionStringAsync();

            var connection = new MySqlConnection(connectionString);

            await connection.OpenAsync();

            return connection;
        }

        private async Task<int> DefaultSaveToDatabase(string commandText, IDictionary<string, object> parameterValues)
        {
            int rowsAffected = 0;

            await WithRetry(5, async () => {
                rowsAffected = await SaveToDatabase(commandText, parameterValues);
            });

            return rowsAffected;
        }

        private async Task<int> SaveToDatabase(string commandText, IDictionary<string, object> parameterValues)
        {
            using (var connection = await CreateAndOpenConnection())
            using (var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
            using (var command = connection.CreateCommand())
            {
                command.CommandText = commandText;
                command.CommandType = CommandType.Text;
                command.Transaction = transaction;

                var parameters = parameterValues.Select(kvp =>
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = kvp.Key;
                    parameter.Value = kvp.Value;
                    return parameter;
                }).ToArray();

                command.Parameters.AddRange(parameters);

                var rows = await command.ExecuteNonQueryAsync();

                await transaction.CommitAsync();

                return rows;
            }
        }

        private async Task WithRetry(int maxAttempts, Func<Task> work)
        {
            int attempt = 0;

            while (attempt++ <= maxAttempts)
            {
                try
                {
                    await work();
                    break;
                }
                catch (Exception e)
                {
                    var message = $"Error occured saving records to database (attempt {attempt} of {maxAttempts})";

                    if (attempt == maxAttempts)
                    {
                        _log.LogError(e, message);
                        throw new Exception(message, e);
                    }
                    else
                    {
                        _log.LogWarning(e, message);
                    }

                    await Task.Delay(attempt * 1000);
                }
            }
        }
    }

}