using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MailCheck.Mx.TlsEvaluator.Rules.TlsEvaluation
{
    public interface IEvaluator<TItem, TResult>
    {
        Task<EvaluationResult<TItem, TResult>> Evaluate(TItem item, Func<List<TResult>, bool> ruleHasFailed = null);
    }

    public class Evaluator<TItem, TResult> : IEvaluator<TItem, TResult>
    {
        private readonly IEnumerable<IGrouping<string, IRule<TItem, TResult>>> _rules;


        public Evaluator(IEnumerable<IRule<TItem, TResult>> rules)
        {
            _rules = rules.OrderBy(_ => _.SequenceNo).GroupBy(_ => _.Category);
        }

        public virtual async Task<EvaluationResult<TItem,TResult>> Evaluate(TItem item, Func<List<TResult>, bool> ruleHasFailed = null)
        {
            List<TResult> errors = new List<TResult>();

            foreach (IGrouping<string, IRule<TItem, TResult>> ruleCategory in _rules)
            {
                foreach (IRule<TItem, TResult> rule in ruleCategory)
                {
                    List<TResult> ruleErrors = await rule.Evaluate(item);

                    errors.AddRange(ruleErrors);

                    if (rule.IsStopRule && (ruleHasFailed?.Invoke(ruleErrors) ?? ruleErrors.Any()))
                    {
                        break;
                    }
                }
            }
            
            return new EvaluationResult<TItem, TResult>(item, errors);
        }
    }
}
