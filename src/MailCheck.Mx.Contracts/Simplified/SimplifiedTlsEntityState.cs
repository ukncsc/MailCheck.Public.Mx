using System;
using System.Collections.Generic;
using MailCheck.Common.Contracts.Advisories;
using MailCheck.Mx.Contracts.SharedDomain;

namespace MailCheck.Mx.Contracts.Simplified
{
    public class SimplifiedTlsEntityState
    {
        public SimplifiedTlsEntityState()
        {
        }

        public SimplifiedTlsEntityState(string hostname, string ipAddress)
        {
            Hostname = hostname;
            IpAddress = ipAddress;
        }

        public string Hostname { get; set; }

        public string IpAddress { get; set; }

        public DateTime? TlsLastUpdated { get; set; }

        public List<NamedAdvisory> TlsAdvisories { get; set; }

        public DateTime? CertsLastUpdated { get; set; }

        public List<NamedAdvisory> CertAdvisories { get; set; }

        public Dictionary<string, string> Certificates { get; set; }

        public List<SimplifiedTlsConnectionResult> SimplifiedTlsConnectionResults { get; set; }
    }
}
