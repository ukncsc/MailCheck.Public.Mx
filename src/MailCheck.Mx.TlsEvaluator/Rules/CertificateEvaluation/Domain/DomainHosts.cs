using System.Collections.Generic;

namespace MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Domain
{
    public class DomainHosts
    {
        public string Domain { get; }
        public List<HostCertificates> HostCertificates { get; }

        public DomainHosts(string domain, List<HostCertificates> hostCertificates)
        {
            Domain = domain;
            HostCertificates = hostCertificates;
        }
    }
}