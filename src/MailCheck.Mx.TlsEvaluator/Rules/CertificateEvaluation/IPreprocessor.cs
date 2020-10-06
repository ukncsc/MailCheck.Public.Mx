using System.Threading.Tasks;

namespace MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation
{
    public interface IPreprocessor<T>
    {
        Task<T> Preprocess(T t);
    }
}