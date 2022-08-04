using Org.BouncyCastle.Crypto.Tls;

namespace MailCheck.Mx.BouncyCastle.OldBouncyCastleTlsApi
{
    internal class EmptyTlsAuthentication : TlsAuthentication
    {
        public TlsCredentials GetClientCredentials(CertificateRequest certificateRequest)
        {
            return null;
        }

        public void NotifyServerCertificate(Certificate serverCertificate)
        {
        }
    }
}