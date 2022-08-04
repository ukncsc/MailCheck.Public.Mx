using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Domain;
using Microsoft.Extensions.Logging;

namespace MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Rules
{
    public class CertificateExpiryShouldBeInDate : IRule<HostCertificates>
    {
        private static readonly IEvaluationErrorFactory CertificateExpiryShouldBeInDateExpired = 
            new EvaluationErrorFactory("ff6971bd-a778-4b21-9b89-4244b20519c6", "mailcheck.tlsCert.certificateExpiryShouldBeInDateExpired", EvaluationErrorType.Error);
        private static readonly IEvaluationErrorFactory CertificateExpiryShouldBeInDateExpiringSoon = 
            new EvaluationErrorFactory("11702da9-46b7-4892-b871-431dbf54342e", "mailcheck.tlsCert.certificateExpiryShouldBeInDateExpiringSoon", EvaluationErrorType.Warning);

        private readonly ILogger<CertificateExpiryShouldBeInDate> _log;
        private readonly TimeSpan _minDays = TimeSpan.FromDays(14);

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
                    errors.Add(CertificateExpiryShouldBeInDateExpired.Create(
                        string.Format(CertificateEvaluatorErrors.CertificateExpiryShouldBeInDateExpired, certificate.CommonName, certificate.ValidTo.ToString("dd/MM/yyyy HH:mm"))));

                }
                else if ((certificate.ValidTo - DateTime.UtcNow) <= _minDays)
                {
                    errors.Add(CertificateExpiryShouldBeInDateExpiringSoon.Create(
                        string.Format(CertificateEvaluatorErrors.CertificateExpiryShouldBeInDateExpiringSoon, certificate.CommonName, certificate.ValidTo.ToString("dd/MM/yyyy HH:mm"))));
                }
            }

            return Task.FromResult(errors);
        }

        public int SequenceNo => 3;

        public bool IsStopRule => false;
    }
}
