using System.Collections.Generic;

namespace MailCheck.Mx.Api.Domain
{
    public class DomainTlsEvaluatorResults
    {
        public DomainTlsEvaluatorResults(string id, bool pending, List<MxTlsEvaluatorResults> mxTlsEvaluatorResults = null, List<MxTlsCertificateEvaluatorResults> certificateResults = null) 
        {
            Id = id;
            Pending = pending;
            CertificateResults = certificateResults;
            MxTlsEvaluatorResults = mxTlsEvaluatorResults ?? new List<MxTlsEvaluatorResults>();
        }

        public string Id { get; }

        public List<MxTlsEvaluatorResults> MxTlsEvaluatorResults { get; }

        public bool Pending { get; }
        public List<MxTlsCertificateEvaluatorResults> CertificateResults { get; }
    }
}