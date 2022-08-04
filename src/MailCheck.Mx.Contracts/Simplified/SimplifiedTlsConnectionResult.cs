namespace MailCheck.Mx.Contracts.Simplified
{
    public class SimplifiedTlsConnectionResult
    {
        public string TestName { get; set; }
        public string CipherSuite { get; set; }
        public string[] CertificateThumbprints { get; set; }
        public string Error { get; set; }
        public string ErrorDescription { get; set; }
        public string SmtpHandshake { get; set; }
    }
}