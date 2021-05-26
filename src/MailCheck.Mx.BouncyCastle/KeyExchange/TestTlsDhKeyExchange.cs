using System.Collections;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Tls;

namespace MailCheck.Mx.BouncyCastle.KeyExchange
{
    internal class TestTlsDhKeyExchange : TlsDHKeyExchange
    {
        public TestTlsDhKeyExchange(int keyExchange, IList supportedSignatureAlgorithms, TlsDHVerifier tlsDHVerifier, DHParameters dhParameters) 
            : base(keyExchange, supportedSignatureAlgorithms, tlsDHVerifier, dhParameters)
        {
        }

        public DHParameters DhParameters => mDHParameters;
    }
}