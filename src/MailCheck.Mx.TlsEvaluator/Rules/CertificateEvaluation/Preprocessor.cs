using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation
{
    public interface IPreprocessorComposite<T> : IPreprocessor<T>{}

    public abstract class Preprocessor<T> : IPreprocessorComposite<T>
    {
        private readonly List<IPreprocessor<T>> _preprocessors;

        protected Preprocessor(IEnumerable<IPreprocessor<T>> preprocessors)
        {
            _preprocessors = preprocessors.ToList();
        }

        public async Task<T> Preprocess(T t)
        {
            foreach (IPreprocessor<T> preprocessor in _preprocessors)
            {
                await preprocessor.Preprocess(t);
            }

            return t;
        }
    }
}