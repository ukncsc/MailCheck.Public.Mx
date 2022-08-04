using System.Threading.Tasks;

namespace MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation
{
    public class NullPreprocessor<T> : IPreprocessorComposite<T>
    {
        public static readonly NullPreprocessor<T> Default = new NullPreprocessor<T>();

        public Task<T> Preprocess(T t)
        {
            return Task.FromResult(t);
        }
    }
}