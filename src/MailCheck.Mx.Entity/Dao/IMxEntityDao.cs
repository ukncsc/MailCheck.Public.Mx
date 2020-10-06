using System.Collections.Generic;
using System.Threading.Tasks;
using MailCheck.Mx.Contracts.Entity;

namespace MailCheck.Mx.Entity.Dao
{
    public interface IMxEntityDao
    {
        Task Save(MxEntityState mxEntityState);
        Task UpdateState(string domain, MxState state);
        Task<MxEntityState> Get(string domainId);
        Task Delete(string domainId);
        Task<List<string>> GetHostsUniqueToDomain(string domainName);
        Task DeleteHosts(List<string> hosts);
    }
}