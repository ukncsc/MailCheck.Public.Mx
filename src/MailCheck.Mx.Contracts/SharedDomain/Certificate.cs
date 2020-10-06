using System;

namespace MailCheck.Mx.Contracts.SharedDomain
{
    public class Certificate
    {
        public Certificate(
            string thumbPrint,
            string issuer,
            string subject, 
            DateTime validFrom, 
            DateTime validTo, 
            string keyAlgoritm,
            int keyLength,
            string serialNumber,
            string version,
            string subjectAlternativeName,
            string commonName
        )
        {
            ThumbPrint = thumbPrint;
            Issuer = issuer;
            Subject = subject;
            ValidFrom = validFrom;
            ValidTo = validTo;
            KeyAlgoritm = keyAlgoritm;
            KeyLength = keyLength;
            SerialNumber = serialNumber;
            Version = version;
            SubjectAlternativeName = subjectAlternativeName;
            CommonName = commonName;
        }

        public string ThumbPrint { get; }
        public string Issuer { get; }
        public string Subject { get; }
        public DateTime ValidFrom { get; }
        public DateTime ValidTo { get; }
        public string KeyAlgoritm { get; }
        public int KeyLength { get; }
        public string SerialNumber { get; }
        public string Version { get; }
        public string SubjectAlternativeName { get; }
        public string CommonName { get; }


        protected bool Equals(Certificate other)
        {
            return string.Equals(ThumbPrint, other.ThumbPrint) && 
                   string.Equals(Issuer, other.Issuer) && 
                   string.Equals(Subject, other.Subject) && 
                   ValidFrom.Equals(other.ValidFrom) && 
                   ValidTo.Equals(other.ValidTo) && 
                   string.Equals(KeyAlgoritm, other.KeyAlgoritm) && 
                   KeyLength == other.KeyLength && 
                   string.Equals(SerialNumber, other.SerialNumber) && 
                   string.Equals(Version, other.Version) && 
                   string.Equals(SubjectAlternativeName, other.SubjectAlternativeName) && 
                   string.Equals(CommonName, other.CommonName);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Certificate) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (ThumbPrint != null ? ThumbPrint.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Issuer != null ? Issuer.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Subject != null ? Subject.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ ValidFrom.GetHashCode();
                hashCode = (hashCode * 397) ^ ValidTo.GetHashCode();
                hashCode = (hashCode * 397) ^ (KeyAlgoritm != null ? KeyAlgoritm.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ KeyLength;
                hashCode = (hashCode * 397) ^ (SerialNumber != null ? SerialNumber.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Version != null ? Version.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (SubjectAlternativeName != null ? SubjectAlternativeName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (CommonName != null ? CommonName.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}