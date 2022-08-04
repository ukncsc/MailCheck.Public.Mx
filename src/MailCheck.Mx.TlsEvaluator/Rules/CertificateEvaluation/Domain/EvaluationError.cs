using System;

namespace MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Domain
{
    public class EvaluationError
    {
        public EvaluationError(Guid id, string name, EvaluationErrorType errorType, string message)
        {
            Id = id;
            Name = name;
            ErrorType = errorType;
            Message = message;
        }

        public EvaluationError(Guid id, string name, EvaluationErrorType errorType, string message, string markdown)
        {
            Id = id;
            Name = name;
            ErrorType = errorType;
            Message = message;
            Markdown = markdown;
        }

        public Guid Id { get; }

        public string Name { get; }

        public EvaluationErrorType ErrorType { get; }

        public string Message { get; }

        public string Markdown { get; }

        protected bool Equals(EvaluationError other)
        {
            return other != null && this != null && Id.Equals(other.Id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((EvaluationError) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) ErrorType * 397) ^ (Message != null ? Message.GetHashCode() : 0);
            }
        }

        public override string ToString()
        {
            return $"{nameof(ErrorType)}: {ErrorType}, {nameof(Message)}: {Message}";
        }
    }
}