using MailCheck.Mx.Contracts.SharedDomain;

namespace MailCheck.Mx.TlsEntity.Dao
{
    public class HostErrors
    {
        public EvaluatorResult?[] ConfigErrors { get; set; }
        public Error[] CertErrors { get; set; }
    }
}