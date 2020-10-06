using System.Collections.Generic;
using System.Threading.Tasks;

namespace MailCheck.Mx.TlsEvaluator.Rules.TlsEvaluation
{
    public interface IRule<in TItem, TResult>
    {
        Task<List<TResult>> Evaluate(TItem t);
        int SequenceNo { get; }
        bool IsStopRule { get; }
        string Category { get; }
    }
}