namespace MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Domain
{
    public class EvaluationError
    {
        public EvaluationError(EvaluationErrorType errorType, string message)
        {
            ErrorType = errorType;
            Message = message;
        }

        public EvaluationErrorType ErrorType { get; }

        public string Message { get; }

        protected bool Equals(EvaluationError other)
        {
            return ErrorType == other.ErrorType && string.Equals(Message, other.Message);
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