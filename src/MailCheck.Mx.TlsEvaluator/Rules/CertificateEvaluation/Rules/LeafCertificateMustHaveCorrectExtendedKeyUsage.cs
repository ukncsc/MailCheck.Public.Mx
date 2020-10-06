using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Domain;
using Microsoft.Extensions.Logging;

namespace MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Rules
{
    public class LeafCertificateMustHaveCorrectExtendedKeyUsage : IRule<HostCertificates>
    {
        private readonly ILogger<LeafCertificateMustHaveCorrectExtendedKeyUsage> _log;

        public LeafCertificateMustHaveCorrectExtendedKeyUsage(ILogger<LeafCertificateMustHaveCorrectExtendedKeyUsage> log)
        {
            _log = log;
        }

        public Task<List<EvaluationError>> Evaluate(HostCertificates hostCertificates)
        {
            List<EvaluationError> errors = new List<EvaluationError>();

            X509Certificate leafCertificate = hostCertificates.Certificates.FirstOrDefault();

            if (HasInvalidExtendedKeyUsage(leafCertificate))
            {
                errors.Add(new EvaluationError(
                    EvaluationErrorType.Error,
                    $"The extended key usage for the certificate with common name {leafCertificate.CommonName} must contain id-kp-serverAuth or anyExtendedKeyUsage to allow it to form a TLS connection."));

                _log.LogInformation($"Found misconfigured extended key usage for host {hostCertificates.Host}.");
            } 
            else 
            {
                _log.LogInformation($"Found correct extended key usage for host {hostCertificates.Host}.");
            }

            return Task.FromResult(errors);
        }

        private bool HasInvalidExtendedKeyUsage(X509Certificate cert) =>
            cert != null &&
            cert.HasExtendedKeyUsage &&
            (!cert.ExtendedKeyUsageIncludesIdKpServerAuth &&
            !cert.ExtendedKeyUsageIncludesAnyExtendedKeyUsage);

        public int SequenceNo => 11;

        public bool IsStopRule => false;
    }
}
