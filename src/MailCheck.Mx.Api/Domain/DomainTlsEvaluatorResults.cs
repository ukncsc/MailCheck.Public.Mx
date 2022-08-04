using System.Collections.Generic;

namespace MailCheck.Mx.Api.Domain
{
    public class DomainTlsEvaluatorResults
    {
        public DomainTlsEvaluatorResults(string id, bool pending, bool tlsRequired, 
            List<MxTlsEvaluatorResults> mxTlsEvaluatorResults = null, 
            List<MxTlsCertificateEvaluatorResults> certificateResults = null,
            List<IpState> associatedIps = null)
        {
            Id = id;
            Pending = pending;
            CertificateResults = certificateResults;
            MxTlsEvaluatorResults = mxTlsEvaluatorResults;
            TlsRequired = tlsRequired;
            AssociatedIps = associatedIps;
        }

        public string Id { get; }

        public List<MxTlsEvaluatorResults> MxTlsEvaluatorResults { get; }

        public bool Pending { get; }

        public List<MxTlsCertificateEvaluatorResults> CertificateResults { get; }

        public bool TlsRequired { get; }

        public List<IpState> AssociatedIps { get; }

    }
}