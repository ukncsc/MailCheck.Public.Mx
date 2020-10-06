using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using MailCheck.Common.Data.Util;
using MailCheck.Common.Data.Abstractions;

namespace MailCheck.Mx.Entity.Seeding.DomainCreated
{
    internal interface IDomainDao
    {
        Task<List<Domain>> GetDomains();
    }

    internal class DomainDao : IDomainDao
    {
        private readonly IConnectionInfo _connectionInfo;

        public DomainDao(IConnectionInfo connectionInfo)
        {
            _connectionInfo = connectionInfo;
        }

        public async Task<List<Domain>> GetDomains()
        {
            List<Domain> list = new List<Domain>();

            string connection = _connectionInfo.ConnectionString;

            using (DbDataReader reader = await MySqlHelper.ExecuteReaderAsync(connection,
                @"SELECT d.name, d.created_date, u.email as created_by FROM domain d LEFT JOIN user u on u.id = d.created_by WHERE d.publish OR d.monitor;"))
            {
                while (reader.Read())
                {
                    list.Add(CreateDomain(reader));
                }
            }

            return list;
        }

        private Domain CreateDomain(DbDataReader reader)
        {
            return new Domain(
                reader.GetString("name"),
                reader.GetString("created_by"),
                reader.GetDateTime("created_date"));
        }
    }
}