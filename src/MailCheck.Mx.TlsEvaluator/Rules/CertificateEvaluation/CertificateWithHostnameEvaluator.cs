using System.Collections.Generic;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Domain;

namespace MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation
{
    public class CertificateWithHostnameEvaluator : Evaluator<HostCertificatesWithName>
    {
        public CertificateWithHostnameEvaluator(IEnumerable<IRule<HostCertificatesWithName>> rules)
            : base(rules, NullPreprocessor<HostCertificatesWithName>.Default) { }
    }
}