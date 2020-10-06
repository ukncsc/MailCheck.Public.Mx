using System;

namespace MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Revocation
{
    public class RevocationInfo
    {
        public static RevocationInfo UnknownRevocationInfo = new RevocationInfo(null, "Unknown");

        public RevocationInfo(DateTime? revocationTime, string revocationReason)
        {
            RevocationTime = revocationTime;
            RevocationReason = revocationReason;
        }

        public DateTime? RevocationTime { get; }
        public string RevocationReason { get; }

        protected bool Equals(RevocationInfo other)
        {
            return RevocationTime.Equals(other.RevocationTime) && string.Equals(RevocationReason, other.RevocationReason);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((RevocationInfo)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (RevocationTime.GetHashCode() * 397) ^ (RevocationReason != null ? RevocationReason.GetHashCode() : 0);
            }
        }

        public override string ToString()
        {
            return $"{nameof(RevocationTime)}: {RevocationTime}, {nameof(RevocationReason)}: {RevocationReason}";
        }
    }
}