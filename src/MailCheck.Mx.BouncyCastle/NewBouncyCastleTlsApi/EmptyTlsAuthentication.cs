using Org.BouncyCastle.Tls;

namespace MailCheck.Mx.BouncyCastle.NewBouncyCastleTlsApi
{
    /// <summary>
    /// No client credentials and no cerificate checking - that is done in the cert evaluator
    /// </summary>
    internal class EmptyTlsAuthentication : TlsAuthentication
    {
        internal static readonly TlsAuthentication Default = new EmptyTlsAuthentication();

        public TlsCredentials GetClientCredentials(CertificateRequest certificateRequest)
        {
            return null;
        }

        public void NotifyServerCertificate(TlsServerCertificate serverCertificate)
        {
        }
    }
}
