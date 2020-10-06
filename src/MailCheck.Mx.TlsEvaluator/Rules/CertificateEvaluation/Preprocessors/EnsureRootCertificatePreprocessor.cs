using System.Linq;
using System.Threading.Tasks;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Domain;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.LookUp;
using Microsoft.Extensions.Logging;

namespace MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Preprocessors
{
    public class EnsureRootCertificatePreprocessor : IPreprocessor<HostCertificates>
    {
        private readonly IRootCertificateLookUp _rootCertificateLookUp;
        private readonly ILogger<EnsureRootCertificatePreprocessor> _log;

        public EnsureRootCertificatePreprocessor(IRootCertificateLookUp rootCertificateLookUp,
            ILogger<EnsureRootCertificatePreprocessor> log)
        {
            _rootCertificateLookUp = rootCertificateLookUp;
            _log = log;
        }

        public async Task<HostCertificates> Preprocess(HostCertificates hostCertificates)
        {
            _log.LogInformation("Running preprocessor {Preprocessor} for host {Host}", nameof(EnsureRootCertificatePreprocessor), hostCertificates.Host);
            bool chainContainsRootCertificate = hostCertificates.Certificates.Any(_ =>
                _.Issuer.ToLower().Trim() == _.Subject.ToLower().Trim());


            if (!chainContainsRootCertificate && hostCertificates.Certificates.Any())
            {
                X509Certificate lastCertificate = hostCertificates.Certificates.Last();

                string issuer = lastCertificate.Issuer;
                string subject = lastCertificate.Subject;

                _log.LogInformation($"Certificate chain missing root certificate. Issuer {issuer} does not match subject {subject} for host {hostCertificates.Host}");

                X509Certificate newRootCertifcate = await _rootCertificateLookUp.GetCertificate(issuer);

                if (newRootCertifcate == null)
                {
                    _log.LogInformation($"Did not find root certificate for issuer {issuer} for host {hostCertificates.Host}");
                }
                else
                {
                    hostCertificates.Certificates.Add(newRootCertifcate);
                    _log.LogInformation($"Added root certificate to chain for host {hostCertificates.Host}");
                }
            }

            return hostCertificates;
        }
    }
}