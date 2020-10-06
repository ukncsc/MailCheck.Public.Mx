using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MailCheck.Mx.Contracts.SharedDomain
{
    public class Error
    {
        public Error(ErrorType errorType, string message)
        {
            ErrorType = errorType;
            Message = message;
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public ErrorType ErrorType { get; }

        public string Message { get; }

        protected bool Equals(Error other)
        {
            return ErrorType == other.ErrorType && string.Equals(Message, other.Message);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Error) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) ErrorType * 397) ^ (Message != null ? Message.GetHashCode() : 0);
            }
        }
    }
}