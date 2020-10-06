using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Domain;
using Microsoft.Extensions.Logging;

namespace MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Rules
{
    public class AllCertificatesShouldBePresent : IRule<HostCertificates>
    {
        private readonly ILogger<AllCertificatesShouldBePresent> _log;

        public AllCertificatesShouldBePresent(ILogger<AllCertificatesShouldBePresent> log)
        {
            _log = log;
        }

        public Task<List<EvaluationError>> Evaluate(HostCertificates certificates)
        {
            _log.LogInformation("Running rule {RuleNumber}:{Rule} for host {Host}", SequenceNo, nameof(AllCertificatesShouldBePresent), certificates.Host);

            List<string> issuers = certificates.Certificates.Select(_ => _.Issuer.Trim()).ToList();

            List<string> subjects = certificates.Certificates.Select(_ => _.Subject.Trim()).ToList();

            return Task.FromResult(issuers.Except(subjects, StringComparer.OrdinalIgnoreCase)
                .Select(_ => new EvaluationError(EvaluationErrorType.Error, string.Format(CertificateEvaluatorErrors.AllCertificatesShouldBePresent, _)))
                .ToList());
        }

        public int SequenceNo => 4;
        
        public bool IsStopRule => true;
    }
}
