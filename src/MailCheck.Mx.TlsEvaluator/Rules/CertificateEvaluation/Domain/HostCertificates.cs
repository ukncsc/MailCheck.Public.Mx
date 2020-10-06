using System.Collections.Generic;
using MailCheck.Mx.Contracts.SharedDomain;

namespace MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Domain
{
    public class HostCertificates
    {
        public HostCertificates(string host, bool hostNotFound, List<X509Certificate> certificates,
            List<SelectedCipherSuite> selectedCipherSuites)
        {
            Host = host;
            HostNotFound = hostNotFound;
            Certificates = certificates;
            SelectedCipherSuites = selectedCipherSuites;
        }

        public string Host { get; }

        public bool HostNotFound { get; }

        public List<X509Certificate> Certificates { get; }

        public List<SelectedCipherSuite> SelectedCipherSuites { get; }
    }
}