using System.Collections.Generic;
using MailCheck.Common.Contracts.Advisories;
using MailCheck.Mx.Contracts.SharedDomain;
using Message = MailCheck.Common.Messaging.Abstractions.Message;

namespace MailCheck.Mx.Contracts.Simplified
{
    public class SimplifiedHostCertificateEvaluated : Message
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id">The IP</param>
        /// <remarks>Do not change the name of the id parameter as it will break deserialization</remarks>
        public SimplifiedHostCertificateEvaluated(string id) : base(id)
        {
        }

        public List<string> Hostnames { get; set; }

        public List<NamedAdvisory> CertificateAdvisoryMessages { get; set; }

        public Dictionary<string, List<NamedAdvisory>> HostSpecificCertificateAdvisoryMessages { get; set; }

        public Dictionary<string, string> Certificates { get; set; }

        public string RootCertificateThumbprint { get; set; }
    }
}