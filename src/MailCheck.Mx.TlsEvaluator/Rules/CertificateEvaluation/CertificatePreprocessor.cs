using System.Collections.Generic;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Domain;

namespace MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation
{
    public class CertificatePreprocessor : Preprocessor<HostCertificates>
    {
        public CertificatePreprocessor(IEnumerable<IPreprocessor<HostCertificates>> preprocessors) 
            : base(preprocessors)
        {
        }
    }
}