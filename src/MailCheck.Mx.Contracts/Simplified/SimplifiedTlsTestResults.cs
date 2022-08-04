using System.Collections.Generic;
using MailCheck.Common.Contracts.Advisories;
using MailCheck.Mx.Contracts.SharedDomain;
using Message = MailCheck.Common.Messaging.Abstractions.Message;

namespace MailCheck.Mx.Contracts.Simplified
{
    public class SimplifiedTlsTestResults : Message
    { 
        public SimplifiedTlsTestResults(string id) : base(id)
        {
        }

        public List<NamedAdvisory> AdvisoryMessages { get; set; }

        public List<SimplifiedTlsConnectionResult> SimplifiedTlsConnectionResults { get; set; }

        public Dictionary<string, string> Certificates { get; set; }

        public bool Inconclusive { get; set; }
    }
}