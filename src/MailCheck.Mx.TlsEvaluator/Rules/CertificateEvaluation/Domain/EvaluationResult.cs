using System.Collections.Generic;

namespace MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Domain
{
    public class EvaluationResult<T>
    {
        public EvaluationResult(T item, List<EvaluationError> errors)
        {
            Item = item;
            Errors = errors;
        }

        public T Item { get; }

        public List<EvaluationError> Errors { get; }
    }
}