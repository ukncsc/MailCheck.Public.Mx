using System.Collections.Generic;

namespace MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Revocation
{
    public class RevocationResult
    {
        public RevocationResult(string errorMessage)
            : this(errorMessage, null, new List<RevocationInfo>())
        { }

        public RevocationResult(bool revoked, List<RevocationInfo> revocationInfos)
            : this(null, revoked, revocationInfos)
        { }

        private RevocationResult(string errorMessage, bool? revoked, List<RevocationInfo> revocationInfos)
        {
            ErrorMessage = errorMessage;
            Revoked = revoked;
            RevocationInfos = revocationInfos;
        }

        public string ErrorMessage { get; }
        public bool? Revoked { get; }
        public List<RevocationInfo> RevocationInfos { get; }
    }
}