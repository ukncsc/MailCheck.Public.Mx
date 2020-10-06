using System.Collections.Generic;
using System.Threading.Tasks;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Domain;

namespace MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation
{
    public interface IRule<in T>
    {
        Task<List<EvaluationError>> Evaluate(T t);
        int SequenceNo { get; }
        bool IsStopRule { get; }
    }
}