using System.Collections.Generic;
using MailCheck.Common.Messaging.Abstractions;

namespace MailCheck.Mx.Contracts.Simplified
{
    public class SimplifiedHostCertificateResult : Message
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id">The IP</param>
        /// <remarks>Do not change the name of the id parameter as it will break deserialization</remarks>
        public SimplifiedHostCertificateResult(string id) : base(id)
        {
        }

        public List<string> Hostnames { get; set; }

        public List<SimplifiedTlsConnectionResult> SimplifiedTlsConnectionResults { get; set; }

        public Dictionary<string, string> Certificates { get; set; }
    }
}