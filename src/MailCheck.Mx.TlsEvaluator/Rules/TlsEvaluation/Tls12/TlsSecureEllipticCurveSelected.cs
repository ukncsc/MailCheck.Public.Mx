using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Tester;
using MailCheck.Mx.TlsEvaluator.Util;

namespace MailCheck.Mx.TlsEvaluator.Rules.TlsEvaluation.Tls12
{
    public class TlsSecureEllipticCurveSelected : IRule<TlsTestResults, RuleTypedTlsEvaluationResult>
    {
        private readonly string intro = "When testing TLS with a range of elliptic curves {0}";

        public TlsTestType Type => TlsTestType.TlsSecureEllipticCurveSelected;

        public Guid ErrorId1 => Guid.Parse("1FB938E1-5082-4096-8CBC-7F323DC46D00");
        public Guid ErrorId2 => Guid.Parse("9041458A-8FB4-4DC6-B782-A54A6A22C243");
        public Guid ErrorId3 => Guid.Parse("4E997F87-3C51-47CA-8FEA-BEA8BEE0DB6B");
        public Guid ErrorId4 => Guid.Parse("F755083D-149C-475A-AF50-9BE91FEA223D");

        public Task<List<RuleTypedTlsEvaluationResult>> Evaluate(TlsTestResults tlsTestConnectionResults)
        {
            BouncyCastleTlsTestResult tlsConnectionResult = tlsTestConnectionResults.TlsSecureEllipticCurveSelected;

            TlsTestType tlsTestType = TlsTestType.TlsSecureEllipticCurveSelected;

            switch (tlsConnectionResult.TlsError)
            {
                case TlsError.HANDSHAKE_FAILURE:
                case TlsError.PROTOCOL_VERSION:
                case TlsError.INSUFFICIENT_SECURITY:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, Guid.NewGuid(), EvaluatorResult.PASS)
                        .ToTaskList();

                case TlsError.TCP_CONNECTION_FAILED:
                case TlsError.SESSION_INITIALIZATION_FAILED:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId1, EvaluatorResult.INCONCLUSIVE,
                            string.Format(intro,
                                $"we were unable to create a connection to the mail server. We will keep trying, so please check back later. Error description \"{tlsConnectionResult.ErrorDescription}\"."))
                        .ToTaskList();

                case null:
                    break;

                default:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId2, EvaluatorResult.INCONCLUSIVE,
                            string.Format(intro,
                                $"the server responded with an error. Error description \"{tlsConnectionResult.ErrorDescription}\"."))
                        .ToTaskList();
            }

            switch (tlsConnectionResult.CurveGroup)
            {
                case CurveGroup.Unknown:
                case CurveGroup.Secp160k1:
                case CurveGroup.Secp160r1:
                case CurveGroup.Secp160r2:
                case CurveGroup.Secp192k1:
                case CurveGroup.Secp192r1:
                case CurveGroup.Secp224k1:
                case CurveGroup.Secp224r1:
                case CurveGroup.Sect163k1:
                case CurveGroup.Sect163r1:
                case CurveGroup.Sect163r2:
                case CurveGroup.Sect193r1:
                case CurveGroup.Sect193r2:
                case CurveGroup.Sect233k1:
                case CurveGroup.Sect233r1:
                case CurveGroup.Sect239k1:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId3, EvaluatorResult.FAIL,
                            string.Format(intro,
                                $"the server selected {tlsConnectionResult.CurveGroup.GetEnumAsString()} which has a curve length of less than 256 bits."))
                        .ToTaskList();

                case CurveGroup.Secp256k1:
                case CurveGroup.Secp256r1:
                case CurveGroup.Secp384r1:
                case CurveGroup.Secp521r1:
                case CurveGroup.Sect283k1:
                case CurveGroup.Sect283r1:
                case CurveGroup.Sect409k1:
                case CurveGroup.Sect409r1:
                case CurveGroup.Sect571k1:
                case CurveGroup.Sect571r1:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, Guid.NewGuid(), EvaluatorResult.PASS)
                        .ToTaskList();
            }

            return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId4, EvaluatorResult.INCONCLUSIVE,
                    string.Format(intro, "there was a problem and we are unable to provide additional information."))
                .ToTaskList();
        }

        public int SequenceNo => 7;

        public bool IsStopRule => false;

        public string Category => RuleCategory.Tls12;
    }
}