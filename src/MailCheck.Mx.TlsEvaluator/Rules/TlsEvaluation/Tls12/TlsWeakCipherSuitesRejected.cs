using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Tester;
using MailCheck.Mx.TlsEvaluator.Util;

namespace MailCheck.Mx.TlsEvaluator.Rules.TlsEvaluation.Tls12
{
    public class TlsWeakCipherSuitesRejected : IRule<TlsTestResults, RuleTypedTlsEvaluationResult>
    {
        private readonly string intro = "When testing TLS with a list of weak cipher suites";

        public Guid ErrorId1 => Guid.Parse("42DDE642-E82D-4332-81F0-A5A0A6638F84");
        public Guid ErrorId2 => Guid.Parse("BAB13ADC-880E-41D5-9622-65C36824081E");
        public Guid ErrorId3 => Guid.Parse("B5EA9EA4-506E-4E0B-943A-4F05DCD890FE");
        public Guid ErrorId4 => Guid.Parse("04DEF230-20DA-4D0F-B10D-DCF5B09B74D4");

        public Task<List<RuleTypedTlsEvaluationResult>> Evaluate(TlsTestResults tlsTestConnectionResults)
        {
            BouncyCastleTlsTestResult tlsConnectionResult = tlsTestConnectionResults.TlsWeakCipherSuitesRejected;

            TlsTestType tlsTestType = TlsTestType.TlsWeakCipherSuitesRejected;

            switch (tlsConnectionResult.TlsError)
            {
                case TlsError.HANDSHAKE_FAILURE:
                case TlsError.PROTOCOL_VERSION:
                case TlsError.INSUFFICIENT_SECURITY:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, Guid.NewGuid(), EvaluatorResult.PASS).ToTaskList();

                case TlsError.TCP_CONNECTION_FAILED:
                case TlsError.SESSION_INITIALIZATION_FAILED:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId1, EvaluatorResult.INCONCLUSIVE,
                        $"{intro} we were unable to create a connection to the mail server. We will keep trying, so please check back later. Error description \"{tlsConnectionResult.ErrorDescription}\".").ToTaskList();

                case null:
                    break;

                default:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId2, EvaluatorResult.INCONCLUSIVE,
                        $"{intro} the server responded with an error. Error description \"{tlsConnectionResult.ErrorDescription}\".").ToTaskList();
            }

            if (tlsConnectionResult.CipherSuite != null)
            {
                return new RuleTypedTlsEvaluationResult(tlsTestType, new TlsEvaluatedResult(ErrorId3, EvaluatorResult.FAIL, $"{intro} the server accepted the connection and selected {tlsConnectionResult.CipherSuite.GetEnumAsString()}.")).ToTaskList();
            }

            return new RuleTypedTlsEvaluationResult(tlsTestType, new TlsEvaluatedResult(ErrorId4, EvaluatorResult.INCONCLUSIVE, $"{intro} there was a problem and we are unable to provide additional information.")).ToTaskList();
        }

        public int SequenceNo => 8;
        public bool IsStopRule => false;
        public string Category => RuleCategory.Tls12;
    }
}
