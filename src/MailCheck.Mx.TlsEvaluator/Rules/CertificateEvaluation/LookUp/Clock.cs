using System;

namespace MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.LookUp
{
    public interface IClock
    {
        DateTime GetDateTimeUtc();
    }

    public class Clock : IClock
    {
        public DateTime GetDateTimeUtc()
        {
            return DateTime.UtcNow;
        }
    }
}