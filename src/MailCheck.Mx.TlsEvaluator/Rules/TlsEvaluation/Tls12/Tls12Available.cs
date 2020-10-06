using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Tester;
using MailCheck.Mx.TlsEvaluator.Util;

namespace MailCheck.Mx.TlsEvaluator.Rules.TlsEvaluation.Tls12
{
    public class Tls12Available : IRule<TlsTestResults, RuleTypedTlsEvaluationResult>
    {
        private readonly string intro = "When testing TLS 1.2 {0}";

        public Guid ErrorId1 => Guid.Parse("784F7C4E-4FEB-46F3-A365-2811D1A57B01");
        public Guid ErrorId2 => Guid.Parse("8BA8F4BE-2DF7-4C9A-9B7E-C2D8365866D3");

        public Task<List<RuleTypedTlsEvaluationResult>> Evaluate(TlsTestResults tlsTestConnectionResults)
        {
            BouncyCastleTlsTestResult tls12Available = tlsTestConnectionResults.Tls12AvailableWithBestCipherSuiteSelected;

            TlsTestType tlsTestType = TlsTestType.Tls12Available;

            if (!tls12Available.Supported())
            {
                if (tls12Available.IsInconclusive())
                {
                    return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId1, EvaluatorResult.INCONCLUSIVE,
                            string.Format(intro,
                                $"we were unable to create a connection to the mail server. We will keep trying, so please check back later. Error description \"{tls12Available.ErrorDescription}\"."))
                        .ToTaskList();
                }


                return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId2, EvaluatorResult.FAIL,
                        tls12Available.ExplicitlyUnsupported() || tls12Available.HandshakeFailure()
                            ? "This server refused to negotiate using TLS 1.2"
                            : string.Format(intro,
                                $"the server responded with the error \"{tls12Available.ErrorDescription}\"."))
                    .ToTaskList();
            }

            return new RuleTypedTlsEvaluationResult(tlsTestType, Guid.NewGuid(), EvaluatorResult.PASS).ToTaskList();
        }

        public int SequenceNo => 1;
        public bool IsStopRule => true;
        public string Category => RuleCategory.Tls12;
    }
}
