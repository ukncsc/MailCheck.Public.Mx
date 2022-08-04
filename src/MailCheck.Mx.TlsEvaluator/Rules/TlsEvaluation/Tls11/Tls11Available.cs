using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Tester;
using MailCheck.Mx.TlsEvaluator.Util;

namespace MailCheck.Mx.TlsEvaluator.Rules.TlsEvaluation.Tls11
{
    public class Tls11Available : IRule<TlsTestResults, RuleTypedTlsEvaluationResult>
    {
        private readonly string intro = "When testing TLS 1.1 {0}";

        public Guid ErrorId1 => Guid.Parse("B0A5F6C9-4F2F-4090-B285-FE177F7243AE");
        public Guid ErrorId2 => Guid.Parse("B3D82248-4884-409E-8A2C-C385E66F3EB2");

        public Task<List<RuleTypedTlsEvaluationResult>> Evaluate(TlsTestResults tlsTestConnectionResults)
        {
            BouncyCastleTlsTestResult tls11Available =
                tlsTestConnectionResults.Tls11AvailableWithBestCipherSuiteSelected;

            TlsTestType tlsTestType = TlsTestType.Tls11Available;

            if (!tls11Available.Supported())
            {
                //inconclusive
                if (tls11Available.IsInconclusive())
                {
                    return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId1, EvaluatorResult.INCONCLUSIVE,
                            string.Format(intro,
                                $"we were unable to create a connection to the mail server. We will keep trying, so please check back later. Error description \"{tls11Available.ErrorDescription}\"."))
                        .ToTaskList();
                }

                return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId2,
                        EvaluatorResult.INFORMATIONAL,
                        "This server does not support TLS 1.1")
                    .ToTaskList();
            }

            return new RuleTypedTlsEvaluationResult(tlsTestType, Guid.NewGuid(), EvaluatorResult.INFORMATIONAL).ToTaskList();
        }

        public int SequenceNo => 1;
        public bool IsStopRule => true;
        public string Category => RuleCategory.Tls11;
    }
}
