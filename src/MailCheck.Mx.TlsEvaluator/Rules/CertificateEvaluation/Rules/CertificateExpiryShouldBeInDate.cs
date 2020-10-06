using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Domain;
using Microsoft.Extensions.Logging;

namespace MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Rules
{
    public class CertificateExpiryShouldBeInDate : IRule<HostCertificates>
    {
        private readonly ILogger<CertificateExpiryShouldBeInDate> _log;
        private readonly TimeSpan _minDays = TimeSpan.FromDays(7);

        public CertificateExpiryShouldBeInDate(ILogger<CertificateExpiryShouldBeInDate> log)
        {
            _log = log;
        }

        public Task<List<EvaluationError>> Evaluate(HostCertificates hostCertificates)
        {
            _log.LogInformation("Running rule {RuleNumber}:{Rule} for host {Host}", SequenceNo, nameof(CertificateExpiryShouldBeInDate), hostCertificates.Host);
            List<EvaluationError> errors = new List<EvaluationError>();
            foreach (var certificate in hostCertificates.Certificates)
            {
                if (DateTime.UtcNow > certificate.ValidTo)
                {
                    errors.Add(new EvaluationError(EvaluationErrorType.Error,
                        string.Format(CertificateEvaluatorErrors.CertificateExpiryShouldBeInDateExpired, certificate.CommonName, certificate.ValidTo.ToString("dd/MM/yyyy HH:mm"))));

                }
                else if ((certificate.ValidTo - DateTime.UtcNow) <= _minDays)
                {
                    errors.Add(new EvaluationError(EvaluationErrorType.Warning,
                        string.Format(CertificateEvaluatorErrors.CertificateExpiryShouldBeInDateExpiringSoon, certificate.CommonName, certificate.ValidTo.ToString("dd/MM/yyyy HH:mm"))));
                }
            }

            return Task.FromResult(errors);
        }

        public int SequenceNo => 3;

        public bool IsStopRule => false;
    }
}
