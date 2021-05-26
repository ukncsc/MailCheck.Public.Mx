using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Tls;

namespace MailCheck.Mx.BouncyCastle
{
    public class TestTlsDHVerifier : DefaultTlsDHVerifier
    {
        public TestTlsDHVerifier() : base(1024)
        {
        }

        protected override bool CheckGroup(DHParameters dhParameters)
        {
            // Allow all groups
            return true;
        }
    }
}