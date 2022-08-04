using System;
using System.Collections.Generic;
using MailCheck.Common.Contracts.Advisories;
using MailCheck.Mx.Contracts.SharedDomain;

namespace MailCheck.Mx.Contracts.Simplified
{
    public class SimplifiedEmailSecTlsEntityState
    {
        public string Hostname { get; set; }

        public DateTime? TlsLastUpdated { get; set; }

        public List<NamedAdvisory> TlsAdvisories { get; set; }

        public DateTime? CertsLastUpdated { get; set; }

        public List<NamedAdvisory> CertAdvisories { get; set; }

    }
}
