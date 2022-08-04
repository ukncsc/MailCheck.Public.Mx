using MailCheck.Mx.Contracts.SharedDomain;

namespace MailCheck.Mx.SimplifiedTlsTester.Domain
{
    public class TestCriteria
    {
        public string Name { get; set; }
        public TlsVersion Protocol { get; set; }
        public CipherSuite[] CipherSuites { get; set; }
    }
}