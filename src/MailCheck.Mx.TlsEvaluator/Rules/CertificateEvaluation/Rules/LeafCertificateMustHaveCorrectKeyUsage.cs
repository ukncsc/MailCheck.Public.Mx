using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Domain;
using Microsoft.Extensions.Logging;

namespace MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Rules
{
    public class LeafCertificateMustHaveCorrectKeyUsage : IRule<HostCertificates>
    {
        private static readonly IEvaluationErrorFactory LeafCertificateMustHaveCorrectKeyUsageFactory =
            new EvaluationErrorFactory("e2508dcf-05a6-439a-9466-c52df92b02e4", "mailcheck.tlsCert.leafCertificateMustHaveCorrectKeyUsage", EvaluationErrorType.Error);

        private ILogger<LeafCertificateMustHaveCorrectKeyUsage> _log;

        public LeafCertificateMustHaveCorrectKeyUsage(ILogger<LeafCertificateMustHaveCorrectKeyUsage> log)
        {
            _log = log;
        }

        public Task<List<EvaluationError>> Evaluate(HostCertificates hostCertificates)
        {
            List<EvaluationError> errors = new List<EvaluationError>();

            X509Certificate leafCertificate = hostCertificates.Certificates.First();

            if (leafCertificate.HasKeyUsage && !HasCorrectKeyUsage(leafCertificate))
            {
                string curveOrGroupCipherSuite = GetCurveOrGroupCipherSuite(hostCertificates.SelectedCipherSuites);

                if (curveOrGroupCipherSuite != null && !leafCertificate.KeyUsageIncludesDigitalSignature)
                {
                    errors.Add(LeafCertificateMustHaveCorrectKeyUsageFactory.Create(
                        $"The mail server supports the cipher suite {curveOrGroupCipherSuite}. " +
                        "This requires a digital signature but the certificate does not have permission to do this " +
                        "(the digitalSignature bit within the Key Usage extension of the certificate is not set.)"));
                }

                string rsaCipherSuite = GetCipherSuite(hostCertificates.SelectedCipherSuites, "TLS_RSA");

                if (rsaCipherSuite != null && !leafCertificate.KeyUsageIncludesKeyEncipherment)
                {
                    errors.Add(LeafCertificateMustHaveCorrectKeyUsageFactory.Create(
                        $"The mail server supports the cipher suite {rsaCipherSuite}. " +
                        "RSA requires Key Encipherment but the certificate does not have permission to do this " +
                        "(the keyEncipherment bit within the Key Usage extension is not set.)"));
                }

                string dhCipherSuite = GetCipherSuite(hostCertificates.SelectedCipherSuites, "TLS_DH_");

                if (dhCipherSuite != null && !leafCertificate.KeyUsageIncludesKeyAgreement)
                {
                    errors.Add(LeafCertificateMustHaveCorrectKeyUsageFactory.Create(
                        $"The mail server supports the cipher suite {dhCipherSuite}. " +
                        "The certificate does not have permission to do a Diffie-Hellman key agreement " +
                        "(the keyAgreement bit in the Key Usage extension is not set)."));
                }
            }

            _log.LogInformation($"Found {errors.Count} key usage errors for host {hostCertificates.Host}.");

            return Task.FromResult(errors);
        }

        public int SequenceNo => 12;

        public bool IsStopRule => false;

        private string GetCurveOrGroupCipherSuite(List<SelectedCipherSuite> selectedCipherSuites) =>
            selectedCipherSuites?
                .Where(_ =>
                    _.TestName.Equals("TlsSecureEllipticCurveSelected", StringComparison.OrdinalIgnoreCase) ||
                    _.TestName.Equals("TlsSecureDiffieHellmanGroupSelected", StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault(_ => _.CipherSuite != null)?.CipherSuite;

        private string GetCipherSuite(List<SelectedCipherSuite> selectedCipherSuites, string startString) =>
            selectedCipherSuites?
                .Where(_ => 
                    !string.IsNullOrWhiteSpace(_?.CipherSuite) &&
                    (
                        _.TestName.Equals("Tls12AvailableWithSha2HashFunctionSelected",
                            StringComparison.OrdinalIgnoreCase) ||
                        _.TestName.Equals("Tls11AvailableWithBestCipherSuiteSelected",
                            StringComparison.OrdinalIgnoreCase) ||
                        _.TestName.Equals("Tls10AvailableWithBestCipherSuiteSelected",
                            StringComparison.OrdinalIgnoreCase)
                    ))
                .Select(_ => _.CipherSuite)
                .FirstOrDefault(_ => _.StartsWith(startString, StringComparison.OrdinalIgnoreCase));

        private bool HasCorrectKeyUsage(X509Certificate certificate) =>
            certificate != null && certificate.KeyUsageIncludesDigitalSignature &&
            certificate.KeyUsageIncludesKeyAgreement && certificate.KeyUsageIncludesKeyEncipherment;
    }
}
