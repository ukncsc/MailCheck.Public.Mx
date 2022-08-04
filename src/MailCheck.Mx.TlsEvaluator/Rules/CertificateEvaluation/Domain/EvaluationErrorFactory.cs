using System;

namespace MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Domain
{
    public interface IEvaluationErrorFactory
    {
        EvaluationError Create(string message, string markdown = null);
    }

    public class EvaluationErrorFactory : IEvaluationErrorFactory
    {
        public EvaluationErrorFactory() {}

        public EvaluationErrorFactory(string id, string name, EvaluationErrorType errorType)
        {
            Id = new Guid(id);
            Name = name;
            ErrorType = errorType;
        }

        public Guid Id { get; set; }

        public string Name { get; set; }

        public EvaluationErrorType ErrorType { get; set; }

        public EvaluationError Create(string message, string markdown = null)
        {
            return new EvaluationError(Id, Name, ErrorType, message, markdown);
        }
    }
}