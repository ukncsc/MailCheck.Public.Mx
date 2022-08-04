using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Domain;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.LookUp;
using Microsoft.Extensions.Logging;

namespace MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Rules
{
    public class RootCertificateShouldBeTrusted : IRule<HostCertificates>
    {
        private static readonly IEvaluationErrorFactory RootCertificateShouldBeTrustedFactory = 
            new EvaluationErrorFactory("b2ccfebc-3339-4ad4-be54-fe1311f034f7", "mailcheck.tlsCert.rootCertificateShouldBeTrusted", EvaluationErrorType.Error);

        private readonly IRootCertificateLookUp _rootCertificateLookUp;
        private readonly ILogger<RootCertificateShouldBeTrusted> _log;

        public RootCertificateShouldBeTrusted(IRootCertificateLookUp rootCertificateLookUp, ILogger<RootCertificateShouldBeTrusted> log)
        {
            _rootCertificateLookUp = rootCertificateLookUp;
            _log = log;
        }

        public async Task<List<EvaluationError>> Evaluate(HostCertificates hostCertificates)
        {
            _log.LogInformation("Running rule {RuleNumber}:{Rule} for host {Host}", SequenceNo, nameof(RootCertificateShouldBeTrusted), hostCertificates.Host);
            X509Certificate trustedRootCertificate = await _rootCertificateLookUp
                .GetCertificate(hostCertificates.Certificates.Last().Issuer);

            return trustedRootCertificate == null 
                ? new List<EvaluationError>{ RootCertificateShouldBeTrustedFactory.Create(string.Format(CertificateEvaluatorErrors.RootCertificateShouldBeTrusted, hostCertificates.Certificates.Last().CommonName))}
                : new List<EvaluationError>();
        }

        public int SequenceNo => 7;
        public bool IsStopRule => true;
    }
}