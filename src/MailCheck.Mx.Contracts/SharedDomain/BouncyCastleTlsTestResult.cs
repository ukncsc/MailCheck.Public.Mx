using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using MailCheck.Mx.BouncyCastle;
using Newtonsoft.Json;

namespace MailCheck.Mx.Contracts.SharedDomain
{
    public class BouncyCastleTlsTestResult
    {
        public BouncyCastleTlsTestResult(TlsError tlsError, string errorDescription, List<string> smtpResponses)
            : this(null, null, null, null, tlsError, errorDescription, smtpResponses)
        { }


        [JsonConstructor]
        public BouncyCastleTlsTestResult(TlsVersion? version,
            CipherSuite? cipherSuite,
            CurveGroup? curveGroup,
            SignatureHashAlgorithm? signatureHashAlgorithm,
            TlsError? tlsError,
            string errorDescription,
            List<string> smtpResponses
        ) : this(version, cipherSuite, curveGroup, signatureHashAlgorithm, tlsError, errorDescription, smtpResponses, null)
        {

        }

        public BouncyCastleTlsTestResult(TlsVersion? version,
            CipherSuite? cipherSuite,
            CurveGroup? curveGroup,
            SignatureHashAlgorithm? signatureHashAlgorithm,
            TlsError? tlsError,
            string errorDescription,
            List<string> smtpResponses,
            List<X509Certificate2> certificates = null
        )
        {
            Version = version;
            CipherSuite = cipherSuite;
            CurveGroup = curveGroup;
            SignatureHashAlgorithm = signatureHashAlgorithm;
            Certificates = certificates ?? new List<X509Certificate2>();
            TlsError = tlsError;
            ErrorDescription = errorDescription;
            SmtpResponses = smtpResponses;
        }
        
        public StartTlsResult SessionInitialisationResult { get; set; }
        public TlsVersion? Version { get; }
        public CipherSuite? CipherSuite { get; }
        public CurveGroup? CurveGroup { get; }
        public SignatureHashAlgorithm? SignatureHashAlgorithm { get; }
        public TlsError? TlsError { get; }
        public string ErrorDescription { get; }
        public List<string> SmtpResponses { get; }

        [JsonIgnore]
        public List<X509Certificate2> Certificates { get; }

        protected bool Equals(BouncyCastleTlsTestResult other)
        {
            return Version == other.Version &&
                   CipherSuite == other.CipherSuite &&
                   CurveGroup == other.CurveGroup &&
                   SignatureHashAlgorithm == other.SignatureHashAlgorithm &&
                   TlsError == other.TlsError &&
                   string.Equals(ErrorDescription, other.ErrorDescription) &&
                   Equals(SmtpResponses, other.SmtpResponses);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((BouncyCastleTlsTestResult)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Version.GetHashCode();
                hashCode = (hashCode * 397) ^ CipherSuite.GetHashCode();
                hashCode = (hashCode * 397) ^ CurveGroup.GetHashCode();
                hashCode = (hashCode * 397) ^ SignatureHashAlgorithm.GetHashCode();
                hashCode = (hashCode * 397) ^ TlsError.GetHashCode();
                hashCode = (hashCode * 397) ^ (ErrorDescription != null ? ErrorDescription.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (SmtpResponses != null ? SmtpResponses.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
