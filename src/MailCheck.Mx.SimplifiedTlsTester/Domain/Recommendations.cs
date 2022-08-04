using MailCheck.Mx.Contracts.SharedDomain;

namespace MailCheck.Mx.SimplifiedTlsTester.Tests
{
    public static class Recommendations
    {
        public static readonly CipherSuite[] GoodForTls13CipherSuites =
        {
            CipherSuite.TLS_AES_256_GCM_SHA384,
            CipherSuite.TLS_AES_128_GCM_SHA256
        };
        public static readonly CipherSuite[] GoodForTls12CipherSuites =
        {
            CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256,
            CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256,
            CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384,
            CipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384
        };
    }
}