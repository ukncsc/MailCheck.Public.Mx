using System;

namespace MailCheck.Mx.Contracts.SharedDomain
{
    public class TlsEvaluatedResult : Common.Messaging.Abstractions.Message
    {
        public TlsEvaluatedResult(Guid id, EvaluatorResult? result = null, string description = null) : base(id.ToString())
        {
            Result = result;
            Description = description;
        }

        public string Description { get; }

        public EvaluatorResult? Result { get; }

        protected bool Equals(TlsEvaluatedResult other)
        {
            return Result == other.Result && String.Equals(Description, other.Description);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TlsEvaluatedResult)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Description != null ? Description.GetHashCode() : 0) * 397) ^ Result.GetHashCode();
            }
        }
    }
}
