using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Domain;
using Microsoft.Extensions.Logging;

namespace MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Rules
{
    public class HostShouldHaveCertificates : IRule<HostCertificates>
    {
        private readonly ILogger<HostShouldHaveCertificates> _log;

        public HostShouldHaveCertificates(ILogger<HostShouldHaveCertificates> log)
        {
            _log = log;
        }

        public Task<List<EvaluationError>> Evaluate(HostCertificates hostCertificates)
        {
            _log.LogInformation("Running rule {RuleNumber}:{Rule} for host {Host}", SequenceNo, nameof(HostShouldHaveCertificates), hostCertificates.Host);
            List<EvaluationError> evaluationErrors = hostCertificates.Certificates.Any()
                ? new List<EvaluationError>()
                : new List<EvaluationError>
                {
                    new EvaluationError(EvaluationErrorType.Inconclusive,
                        string.Format(CertificateEvaluatorErrors.HostShouldHaveCertificates, hostCertificates.Host))
                };

            return Task.FromResult(evaluationErrors);
        }

        public int SequenceNo => 1;
        public bool IsStopRule => true;
    }
}