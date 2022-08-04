using System.Collections.Generic;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Tester;
using MailCheck.Mx.TlsTester.Util;

namespace MailCheck.Mx.TlsTester.Tls.Tests
{
    public class Tls13AvailableWithBestCipherSuiteSelected : ITlsTest
    {
        public virtual int Id => (int)TlsTestType.Tls13AvailableWithBestCipherSuiteSelected;

        public virtual string Name => nameof(Tls13AvailableWithBestCipherSuiteSelected);

        public virtual TlsVersion Version => TlsVersion.TlsV13;

        public virtual List<CipherSuite> CipherSuites => new List<CipherSuite>
        {
            CipherSuite.TLS_AES_256_GCM_SHA384,
            CipherSuite.TLS_AES_128_GCM_SHA256,
            CipherSuite.TLS_AES_128_CCM_SHA256,
            CipherSuite.TLS_AES_128_CCM_8_SHA256,
            CipherSuite.TLS_CHACHA20_POLY1305_SHA256,
            CipherSuite.TLS_SM4_GCM_SM3,
            CipherSuite.TLS_SM4_CCM_SM3,
        };
    }
}

