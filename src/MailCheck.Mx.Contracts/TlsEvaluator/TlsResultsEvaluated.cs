using MailCheck.Mx.Contracts.SharedDomain;
using Message = MailCheck.Common.Messaging.Abstractions.Message;

namespace MailCheck.Mx.Contracts.TlsEvaluator
{
    public class TlsResultsEvaluated : Message
    {
        public TlsResultsEvaluated(string id, bool failed, TlsRecords tlsRecords, CertificateResults certificates = null) : base(id)
        {
            TlsRecords = tlsRecords;
            Certificates = certificates;
            Failed = failed;
        }

        public CertificateResults Certificates { get; }
        public bool Failed { get; }
        public TlsRecords TlsRecords { get; }
    }
}