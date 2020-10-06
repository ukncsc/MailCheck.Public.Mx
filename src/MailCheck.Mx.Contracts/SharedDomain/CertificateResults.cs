using System;
using System.Collections.Generic;

namespace MailCheck.Mx.Contracts.SharedDomain
{
    public class CertificateResults : IEquatable<CertificateResults>
    {
        public CertificateResults(List<Certificate> certificates, List<Error> errors)
        {
            Certificates = certificates ?? new List<Certificate>();
            Errors = errors ?? new List<Error>();
        }


        public List<Certificate> Certificates { get; }

        public List<Error> Errors { get; }

        public bool Equals(CertificateResults other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Certificates, other.Certificates) && Equals(Errors, other.Errors);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CertificateResults) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Certificates != null ? Certificates.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Errors != null ? Errors.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(CertificateResults left, CertificateResults right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(CertificateResults left, CertificateResults right)
        {
            return !Equals(left, right);
        }
    }
}