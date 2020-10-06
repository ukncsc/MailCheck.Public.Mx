using System.Collections.Generic;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Domain;

namespace MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation
{
    public class CertificateEvaluator : Evaluator<HostCertificates>
    {
        public CertificateEvaluator(IEnumerable<IRule<HostCertificates>> rules, IPreprocessorComposite<HostCertificates> preprocessor)
            : base(rules, preprocessor){}
    }
}