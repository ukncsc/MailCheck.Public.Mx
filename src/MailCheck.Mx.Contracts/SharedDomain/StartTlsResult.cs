using System.Collections.Generic;

namespace MailCheck.Mx.BouncyCastle
{
    public class StartTlsResult
    {
        public StartTlsResult(bool success, List<string> smtpSession, string error)
        {
            Success = success;
            SmtpSession = smtpSession ?? new List<string>();
            Error = error;
        }

        public bool Success { get; }
        public List<string> SmtpSession { get; }
        public string Error { get; }
    }
}