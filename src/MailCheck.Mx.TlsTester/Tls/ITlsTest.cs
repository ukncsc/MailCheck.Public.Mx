using System.Collections.Generic;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Tester;

namespace MailCheck.Mx.TlsTester.Tls
{
    public interface ITlsTest
    {
        int Id { get; }
        string Name { get; }
        TlsVersion Version { get; }
        List<CipherSuite> CipherSuites { get; }
    }
}
