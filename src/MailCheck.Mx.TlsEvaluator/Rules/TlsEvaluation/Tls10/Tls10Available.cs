using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Tester;
using MailCheck.Mx.TlsEvaluator.Util;

namespace MailCheck.Mx.TlsEvaluator.Rules.TlsEvaluation.Tls10
{
    public class Tls10Available : IRule<TlsTestResults, RuleTypedTlsEvaluationResult>
    {
        private readonly string intro = "When testing TLS 1.0 {0}";

        public Guid ErrorId1 => Guid.Parse("0786704C-A1ED-40F6-B95B-B00F66E698B3");
        public Guid ErrorId2 => Guid.Parse("582EA255-CD37-4C4D-AAB4-556CEA049FBC");

        public Task<List<RuleTypedTlsEvaluationResult>> Evaluate(TlsTestResults tlsTestConnectionResults)
        {
            BouncyCastleTlsTestResult tls10Available = tlsTestConnectionResults.Tls10AvailableWithBestCipherSuiteSelected;

            TlsTestType tlsTestType = TlsTestType.Tls10Available;

            if (!tls10Available.Supported())
            {
                if (tls10Available.TlsError != null)
                {
                    //inconclusive
                    if (tls10Available.IsInconclusive())
                    {
                        return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId1, EvaluatorResult.INCONCLUSIVE,
                                string.Format(intro,
                                    $"we were unable to create a connection to the mail server. We will keep trying, so please check back later. Error description \"{tls10Available.ErrorDescription}\"."))
                            .ToTaskList();
                    }

                    return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId2,
                            tls10Available.ExplicitlyUnsupported() || tls10Available.HandshakeFailure()
                                ? EvaluatorResult.INFORMATIONAL
                                : EvaluatorResult.WARNING,
                            tls10Available.ExplicitlyUnsupported() || tls10Available.HandshakeFailure()
                                ? "This server refused to negotiate using TLS 1.0"
                                : string.Format(intro,
                                    $"the server responded with the error \"{tls10Available.ErrorDescription}\"."))
                        .ToTaskList();
                }
            }

            return new RuleTypedTlsEvaluationResult(tlsTestType, Guid.NewGuid(), EvaluatorResult.PASS).ToTaskList();
            
        }

        public int SequenceNo => 1;
        public bool IsStopRule => true;
        public string Category => RuleCategory.Tls10;
    }
}