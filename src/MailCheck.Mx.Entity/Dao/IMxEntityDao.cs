using System.Collections.Generic;
using System.Threading.Tasks;
using MailCheck.Mx.Contracts.Entity;
using MailCheck.Mx.Contracts.Simplified;

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
        Task<List<SimplifiedTlsEntityState>> GetSimplifiedStates(string hostName);
    }
}