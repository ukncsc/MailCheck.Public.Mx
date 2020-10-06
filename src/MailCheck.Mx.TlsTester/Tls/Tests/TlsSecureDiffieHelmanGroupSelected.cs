using System.Collections.Generic;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Tester;
using MailCheck.Mx.TlsTester.Util;

namespace MailCheck.Mx.TlsTester.Tls.Tests
{
    public class TlsSecureDiffieHelmanGroupSelected : ITlsTest
    {
        public int Id => (int)TlsTestType.TlsSecureDiffieHellmanGroupSelected;

        public string Name => nameof(TlsSecureDiffieHelmanGroupSelected);

        public TlsVersion Version => TlsVersion.TlsV12;

        public List<CipherSuite> CipherSuites => new List<CipherSuite>
        {
            CipherSuite.TLS_DHE_RSA_WITH_AES_256_GCM_SHA384,
            CipherSuite.TLS_DHE_RSA_WITH_AES_128_GCM_SHA256,
            CipherSuite.TLS_DHE_RSA_WITH_AES_256_CBC_SHA,
            CipherSuite.TLS_DHE_RSA_WITH_AES_128_CBC_SHA,
        };
    }
}