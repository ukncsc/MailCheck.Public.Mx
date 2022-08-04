using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Domain;
using Microsoft.Extensions.Logging;

namespace MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Rules
{
    public class AllCertificatesSignaturesShouldBeValid : IRule<HostCertificates>
    {
        private static readonly IEvaluationErrorFactory AllCertificatesSignaturesShouldBeValidFactory = 
            new EvaluationErrorFactory("009b977e-9fbf-4dcf-96c3-f250f3f853e7", "mailcheck.tlsCert.allCertificatesSignaturesShouldBeValid", EvaluationErrorType.Error);

        private readonly ILogger<AllCertificatesSignaturesShouldBeValid> _log;

        public AllCertificatesSignaturesShouldBeValid(ILogger<AllCertificatesSignaturesShouldBeValid> log)
        {
            _log = log;
        }

        public Task<List<EvaluationError>> Evaluate(HostCertificates hostCertificates)
        {
            _log.LogInformation("Running rule {RuleNumber}:{Rule} for host {Host}", SequenceNo, nameof(AllCertificatesSignaturesShouldBeValid), hostCertificates.Host);
            List<EvaluationError> errors = new List<EvaluationError>();
            for (int i = 0; i < hostCertificates.Certificates.Count - 1; i++)
            {
                try
                {
                    hostCertificates.Certificates[i].VerifySignature(hostCertificates.Certificates[i + 1].PublicKey, 
                        hostCertificates.Certificates[i + 1].PublicKeyIdentifier);
                }
                catch (Exception)
                {
                    errors.Add(AllCertificatesSignaturesShouldBeValidFactory.Create(
                        string.Format(CertificateEvaluatorErrors.AllCertificatesSignaturesShouldBeValid, hostCertificates.Certificates[i].CommonName)));
                }
            }

            return Task.FromResult(errors);
        }

        public int SequenceNo => 8;
        public bool IsStopRule => true;
    }
}
