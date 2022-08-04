using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Domain;
using Microsoft.Extensions.Logging;

namespace MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Rules
{
    public class RootAndIntermediateCertificatesMustHaveKeyCertSign : IRule<HostCertificates>
    {
        private static readonly IEvaluationErrorFactory NoKeyUsageExtension = 
            new EvaluationErrorFactory("22ceefbb-7fc6-4f51-91af-455de0baa473", "mailcheck.tlsCert.noKeyUsageExtension", EvaluationErrorType.Inconclusive);
        private static readonly IEvaluationErrorFactory RootAndIntermediateCertificatesMustHaveKeyCertSignFactory = 
            new EvaluationErrorFactory("13d80fd0-7d55-42ab-90b4-df39ed91f88f", "mailcheck.tlsCert.rootAndIntermediateCertificatesMustHaveKeyCertSign", EvaluationErrorType.Error);


        private readonly ILogger<RootAndIntermediateCertificatesMustHaveKeyCertSign> _log;

        public RootAndIntermediateCertificatesMustHaveKeyCertSign(ILogger<RootAndIntermediateCertificatesMustHaveKeyCertSign> log)
        {
            _log = log;
        }

        public Task<List<EvaluationError>> Evaluate(HostCertificates hostCertificates)
        {
            List<EvaluationError> errors = new List<EvaluationError>();

            List<X509Certificate> intermediateAndRootCertificates = hostCertificates.Certificates.Skip(1).ToList();

            errors.AddRange(
                intermediateAndRootCertificates
                    .Where(_ => _ != null && _.HasKeyUsage && !_.KeyUsageIncludesKeyCertSign)
                    .Select(_ => RootAndIntermediateCertificatesMustHaveKeyCertSignFactory.Create(
                        $"The certificate with common name {_.CommonName} does not have the KeyCertSign extension flag present and therefore is not allowed to sign certificates.")));

            errors.AddRange(
                intermediateAndRootCertificates
                    .Where(_ => _ != null && !_.HasKeyUsage && !(_.Issuer == _.Subject))
                    .Select(_ => NoKeyUsageExtension.Create(
                        $"The certificate with common name {_.CommonName} does not have the key usage extension. This extension is required for root and intermediate certificates and should contain the KeyCertSign extension flag.")));

            _log.LogInformation($"Found {errors.Count} KeyCertSign issues for host {hostCertificates.Host}.");

            return Task.FromResult(errors);
        }

        public int SequenceNo => 10;

        public bool IsStopRule => false;
    }
}
