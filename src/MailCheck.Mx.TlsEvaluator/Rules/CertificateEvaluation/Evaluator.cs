using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Domain;

namespace MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation
{
    public interface IEvaluator<T>
    {
        Task<EvaluationResult<T>> Evaluate(T item);
    }

    public abstract class Evaluator<T> : IEvaluator<T>
    {
        private readonly IPreprocessorComposite<T> _preprocessor;
        private readonly List<IRule<T>> _rules;

        protected Evaluator(IEnumerable<IRule<T>> rules, IPreprocessorComposite<T> preprocessor)
        {
            _preprocessor = preprocessor;
            _rules = rules.OrderBy(_ => _.SequenceNo).ToList();
        }

        public virtual async Task<EvaluationResult<T>> Evaluate(T item)
        {
            await _preprocessor.Preprocess(item);

            List<EvaluationError> errors = new List<EvaluationError>();
            foreach (IRule<T> rule in _rules)
            {
                List<EvaluationError> ruleErrors = await rule.Evaluate(item);

                if (ruleErrors.Any())
                {
                    errors.AddRange(ruleErrors);

                    if (rule.IsStopRule)
                    {
                        break;
                    }
                }
            }
            return new EvaluationResult<T>(item, errors);
        }
    }
}