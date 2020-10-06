using Org.BouncyCastle.Crypto.Tls;

namespace MailCheck.Mx.BouncyCastle
{
    internal class BasicTlsClient : DefaultTlsClient
    {
        public override TlsAuthentication GetAuthentication()
        {
            return new EmptyTlsAuthentication();
        }
    }
}