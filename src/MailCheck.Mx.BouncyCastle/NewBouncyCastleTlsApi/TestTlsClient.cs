using System;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Tls;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;

namespace MailCheck.Mx.BouncyCastle.NewBouncyCastleTlsApi
{
    internal class TestTlsClient : DefaultTlsClient
    {
        private readonly ProtocolVersion[] protocolVersions;
        private readonly int[] cipherSuites;

        public TestTlsClient(ProtocolVersion[] protocolVersions, int[] cipherSuites) 
            : base(new BcTlsCrypto(SecureRandom.GetInstance("SHA256PRNG")))
        {
            this.protocolVersions = protocolVersions;
            this.cipherSuites = cipherSuites;
        }

        public override TlsAuthentication GetAuthentication()
        {
            return EmptyTlsAuthentication.Default;
        }

        protected override ProtocolVersion[] GetSupportedVersions()
        {
            return protocolVersions ?? base.GetSupportedVersions();
        }

        protected override int[] GetSupportedCipherSuites()
        {
            return cipherSuites ?? base.GetSupportedCipherSuites();
        }
    }
}
