using System.Linq;
using MailCheck.Mx.TlsTester.Config;
using Microsoft.Extensions.Logging;
using MailCheck.Common.Util;

namespace MailCheck.Mx.TlsTester.MxTester
{
    public interface IMxSecurityTesterIgnoredHostsFilter
    {
        bool IsIgnored(string host);
    }

    public class MxSecurityTesterIgnoredHostsFilter : IMxSecurityTesterIgnoredHostsFilter
    {
        private readonly ILogger<MxSecurityTesterIgnoredHostsFilter> _log;
        private readonly IMxTesterConfig _config;

        public MxSecurityTesterIgnoredHostsFilter(ILogger<MxSecurityTesterIgnoredHostsFilter> log, IMxTesterConfig config)
        {
            _log = log;
            _config = config;
        }

        public bool IsIgnored(string host)
        {
            host = DomainNameUtils.ReverseDomainName(host);

            string[] blockedHosts = _config.TlsTesterIgnoredHosts;

            if (blockedHosts.Any(blockedHostPrefix => host.StartsWith(blockedHostPrefix))) {

                return true;
            }

            return false;
        }
    }
}