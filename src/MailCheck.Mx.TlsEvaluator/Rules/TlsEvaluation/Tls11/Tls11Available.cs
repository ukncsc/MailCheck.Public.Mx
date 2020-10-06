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
        public Guid ErrorId3 => Guid.Parse("C9E06A53-529B-4CEC-8883-7748E9DEEC37");
        public Guid ErrorId4 => Guid.Parse("91581B2C-C2F6-4028-9A2D-782947DC9CCF");
        public Guid ErrorId5 => Guid.Parse("481FB297-D8B0-4171-BCC4-7242BA81045B");
        public Guid ErrorId6 => Guid.Parse("75E9F65E-4D51-401E-9593-65F070F429A8");
        public Guid ErrorId7 => Guid.Parse("7ADDB1F3-6628-4938-B0CE-68C6811794D8");
        public Guid ErrorId8 => Guid.Parse("770E394A-25A2-4BC9-93E4-C82AA5E33992");
        public Guid ErrorId9 => Guid.Parse("583CA67A-F464-4D7F-AE7F-CD62F0C0D8BB");

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
                        tls11Available.ExplicitlyUnsupported() || tls11Available.HandshakeFailure()
                            ? EvaluatorResult.INFORMATIONAL
                            : EvaluatorResult.WARNING,
                        tls11Available.ExplicitlyUnsupported() || tls11Available.HandshakeFailure()
                            ? "This server refused to negotiate using TLS 1.1"
                            : string.Format(intro,
                                $"the server responded with the error \"{tls11Available.ErrorDescription}\"."))
                    .ToTaskList();
            }

            return new RuleTypedTlsEvaluationResult(tlsTestType, Guid.NewGuid(), EvaluatorResult.PASS).ToTaskList();
        }

        public int SequenceNo => 1;
        public bool IsStopRule => true;
        public string Category => RuleCategory.Tls11;
    }
}
