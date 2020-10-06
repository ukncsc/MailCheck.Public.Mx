﻿using System.Collections;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Tls;

namespace MailCheck.Mx.BouncyCastle.KeyExchange
{
    internal class TestTlsDhKeyExchange : TlsDHKeyExchange
    {
        public TestTlsDhKeyExchange(int keyExchange, IList supportedSignatureAlgorithms, DHParameters dhParameters) 
            : base(keyExchange, supportedSignatureAlgorithms, dhParameters)
        {
        }

        public DHParameters DhParameters => mDHParameters;
    }
}