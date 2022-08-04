using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Domain;
using Microsoft.Extensions.Logging;

namespace MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Rules
{
    public class AllCertificatesShouldBeInOrder : IRule<HostCertificates>
    {
        private static readonly IEvaluationErrorFactory AllCertificatesShouldBeInOrderFactory = 
            new EvaluationErrorFactory("c7ea84ad-8d5f-4899-896e-f40a8a449bba", "mailcheck.tlsCert.allCertificatesShouldBeInOrder", EvaluationErrorType.Error );

        private readonly ILogger<AllCertificatesShouldBeInOrder> _log;

        public AllCertificatesShouldBeInOrder(ILogger<AllCertificatesShouldBeInOrder> log)
        {
            _log = log;
        }

        public Task<List<EvaluationError>> Evaluate(HostCertificates hostCertificates)
        {
            _log.LogInformation("Running rule {RuleNumber}:{Rule} for host {Host}", SequenceNo, nameof(AllCertificatesShouldBeInOrder), hostCertificates.Host);
            List<EvaluationError> error = new List<EvaluationError>();
            for (int i = 0; i < hostCertificates.Certificates.Count; i++)
            {
                string currentIssuer = hostCertificates.Certificates[i].Issuer;
                string nextSubject = hostCertificates.Certificates.ElementAtOrDefault(i + 1)?.Subject;

                if (nextSubject != null)
                {
                    if (currentIssuer.Trim().ToLower() != nextSubject.Trim().ToLower())
                    {
                        error.Add(AllCertificatesShouldBeInOrderFactory.Create(CertificateEvaluatorErrors.AllCertificatesShouldBeInOrder));
                        break;
                    }
                }
                else if (currentIssuer.Trim().ToLower() != hostCertificates.Certificates[i].Subject.Trim().ToLower())
                {
                    error.Add(AllCertificatesShouldBeInOrderFactory.Create(CertificateEvaluatorErrors.AllCertificatesShouldBeInOrder));
                    break;
                }
            }

            return Task.FromResult(error);
        }

        public int SequenceNo => 5;
        public bool IsStopRule => true;
    }
}
