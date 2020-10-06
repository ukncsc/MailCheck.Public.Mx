using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Domain;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Revocation;
using Microsoft.Extensions.Logging;

namespace MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Rules
{
    public class NonRootCertificatesShouldNotAppearOnRevocationLists : IRule<HostCertificates>
    {
        private readonly IOcspValidator _ocspValidator;
        private readonly ICrlValidator _crlValidator;
        private readonly ILogger<NonRootCertificatesShouldNotAppearOnRevocationLists> _log;

        public NonRootCertificatesShouldNotAppearOnRevocationLists(IOcspValidator ocspValidator, ICrlValidator crlValidator, ILogger<NonRootCertificatesShouldNotAppearOnRevocationLists> log)
        {
            _ocspValidator = ocspValidator;
            _crlValidator = crlValidator;
            _log = log;
        }

        public async Task<List<EvaluationError>> Evaluate(HostCertificates hostCertificates)
        {
            _log.LogInformation("Running rule {RuleNumber}:{Rule} for host {Host}", SequenceNo, nameof(NonRootCertificatesShouldNotAppearOnRevocationLists), hostCertificates.Host);
            for (int i = hostCertificates.Certificates.Count - 1; i > 0; i--)
            {
                RevocationResult ocspResult = await _ocspValidator.CheckOcspRevocation(hostCertificates.Host, hostCertificates.Certificates[i - 1], hostCertificates.Certificates[i]);
                if (ocspResult.Revoked.HasValue)
                {
                    EvaluationError error = GetErrorFromRevocationResult(ocspResult, hostCertificates.Certificates[i - 1]);
                    if (error != null)
                    {
                        return new List<EvaluationError> { error };
                    }
                }
                else
                {
                    RevocationResult crlResult = await _crlValidator.CheckCrlRevocation(hostCertificates.Host, hostCertificates.Certificates[i - 1]);
                    if (crlResult.Revoked.HasValue)
                    {
                        EvaluationError error = GetErrorFromRevocationResult(crlResult, hostCertificates.Certificates[i - 1]);
                        if (error != null)
                        {
                            return new List<EvaluationError>{error};
                        }
                    }
                    else
                    {
                        string errorMessage = $"OCSP Error: {ocspResult.ErrorMessage}{Environment.NewLine}CRL Error: {crlResult.ErrorMessage}";

                        return new List<EvaluationError>{new EvaluationError(EvaluationErrorType.Inconclusive,
                            string.Format(CertificateEvaluatorErrors.NonRootCertificatesShouldNotAppearOnRevocationListsError,
                                hostCertificates.Certificates[i-1].CommonName, Environment.NewLine, errorMessage))};
                    }
                }
            }

            return new List<EvaluationError>();
        }

        private EvaluationError GetErrorFromRevocationResult(RevocationResult result, X509Certificate certificate)
        {
            if (result.Revoked.GetValueOrDefault(false))
            {
                string revocationReasons =
                    string.Join(Environment.NewLine, result.RevocationInfos
                        .Select(_ => $"Revocation Date: {(_.RevocationTime.HasValue ? _.RevocationTime.Value.ToString("dd/MM/yyyy HH:mm") : "unknown")}, Revocation Reason: {_.RevocationReason ?? "unknown"}"));

                return new EvaluationError(EvaluationErrorType.Error, string.Format(CertificateEvaluatorErrors.NonRootCertificatesShouldNotAppearOnRevocationListsRevoked,
                    certificate.CommonName, Environment.NewLine, revocationReasons));
            }

            return null;
        }

        public int SequenceNo => 9;
        public bool IsStopRule => true;
    }
}
