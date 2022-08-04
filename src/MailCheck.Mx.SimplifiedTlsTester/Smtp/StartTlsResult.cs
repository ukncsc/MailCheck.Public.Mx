using System.Collections.Generic;
using MailCheck.Mx.BouncyCastle;

namespace MailCheck.Mx.SimplifiedTlsTester.Smtp
{
    public class SimplifiedStartTlsResult : StartTlsResult
    {
        public SimplifiedStartTlsResult(bool success, List<string> smtpSession, string error, Outcome outcome):base(success, smtpSession, error)
        {
            Outcome = outcome;
        }

        public Outcome Outcome { get; }
    }
}