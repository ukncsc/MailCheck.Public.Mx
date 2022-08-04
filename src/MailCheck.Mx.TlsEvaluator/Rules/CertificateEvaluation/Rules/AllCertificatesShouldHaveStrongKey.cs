using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Domain;
using Microsoft.Extensions.Logging;

namespace MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Rules
{
    public class AllCertificatesShouldHaveStrongKey : IRule<HostCertificates>
    {
        private static readonly IEvaluationErrorFactory AllCertificatesShouldHaveStrongKeyFactory = 
            new EvaluationErrorFactory("4d327b7e-aab1-4fa6-9dd3-855424afd3ca", "mailcheck.tlsCert.allCertificatesShouldHaveStrongKey", EvaluationErrorType.Error);

        private readonly ILogger<AllCertificatesShouldHaveStrongKey> _log;

        // ReSharper disable once InconsistentNaming
        private const string RSA = "rsa";

        // ReSharper disable once InconsistentNaming
        private const int RSALength = 2048;

        // ReSharper disable once InconsistentNaming
        private const string ECC = "ecc";

        // ReSharper disable once InconsistentNaming
        private const int ECCLength = 256;

        public AllCertificatesShouldHaveStrongKey(ILogger<AllCertificatesShouldHaveStrongKey> log)
        {
            _log = log;
        }

        public Task<List<EvaluationError>> Evaluate(HostCertificates hostCertificates)
        {
            _log.LogInformation("Running rule {RuleNumber}:{Rule} for host {Host}", SequenceNo, nameof(AllCertificatesShouldHaveStrongKey), hostCertificates.Host);
            return Task.FromResult(hostCertificates.Certificates.SelectMany(EvaluateKeyLength).ToList());
        }

        private List<EvaluationError> EvaluateKeyLength(X509Certificate certificate)
        {
            int keyLength = GetMinKeySize(certificate.KeyAlgoritm);
            return certificate.KeyLength < keyLength 
                ? new List<EvaluationError>{AllCertificatesShouldHaveStrongKeyFactory.Create(
                    string.Format(CertificateEvaluatorErrors.AllCertificatesShouldHaveStrongKey, certificate.CommonName, certificate.KeyAlgoritm, certificate.KeyLength, keyLength)) } 
                : new List<EvaluationError>();
        }

        private int GetMinKeySize(string key)
        {
            switch (key.ToLower())
            {
                case RSA:
                    return RSALength;
                case ECC:
                    return ECCLength;
                default:
                    throw new ArgumentException($"Unknown key type: {key}");
            }
        }

        public int SequenceNo => 6;
        public bool IsStopRule => false;
    }
}
