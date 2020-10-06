using System.Collections.Generic;
using System.Linq;

namespace MailCheck.Mx.TlsEvaluator.Rules.TlsEvaluation
{
    public class EvaluationResult<TItem, TResult>
    {
        public EvaluationResult(TItem item, params TResult[] messages)
        {
            Item = item;
            Messages = messages.ToList();
        }

        public EvaluationResult(TItem item, List<TResult> messages)
        {
            Item = item;
            Messages = messages;
        }

        public TItem Item { get; }

        public List<TResult> Messages { get; }
    }
}