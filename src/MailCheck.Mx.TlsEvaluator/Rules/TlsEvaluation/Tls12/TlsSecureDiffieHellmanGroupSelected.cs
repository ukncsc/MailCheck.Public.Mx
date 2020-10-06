using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Tester;
using MailCheck.Mx.TlsEvaluator.Util;

namespace MailCheck.Mx.TlsEvaluator.Rules.TlsEvaluation.Tls12
{
    public class TlsSecureDiffieHellmanGroupSelected : IRule<TlsTestResults, RuleTypedTlsEvaluationResult>
    {
        private readonly string advice = "Only groups of 2048 bits or more should be used.";
        private readonly string intro = "When testing TLS with a range of Diffie Hellman groups {0}";

        public Guid ErrorId1 => Guid.Parse("37FACCB8-CD0C-4948-9E26-411E0CD8F683");
        public Guid ErrorId2 => Guid.Parse("0A91E4C2-63A2-43C0-89D7-4C4859A7489B");
        public Guid ErrorId3 => Guid.Parse("03232B04-9444-4FC3-96B6-9A2A0B694B3D");
        public Guid ErrorId4 => Guid.Parse("FD9B6B82-1B51-49E5-A9D2-0CDF55EC205C");
        public Guid ErrorId5 => Guid.Parse("D0E4F93D-1B42-4ED1-A786-7A3E67CA35C7");
        public Guid ErrorId6 => Guid.Parse("2D82CC8F-45E3-4E86-AD53-8B2FB433E1A9");

        public Task<List<RuleTypedTlsEvaluationResult>> Evaluate(TlsTestResults tlsTestConnectionResults)
        {
            BouncyCastleTlsTestResult tlsConnectionResult =
                tlsTestConnectionResults.TlsSecureDiffieHellmanGroupSelected;

            TlsTestType tlsTestType = TlsTestType.TlsSecureDiffieHellmanGroupSelected;

            switch (tlsConnectionResult.TlsError)
            {
                case TlsError.HANDSHAKE_FAILURE:
                case TlsError.PROTOCOL_VERSION:
                case TlsError.INSUFFICIENT_SECURITY:
                    return new RuleTypedTlsEvaluationResult(tlsTestType,
                        new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)).ToTaskList();

                case TlsError.TCP_CONNECTION_FAILED:
                case TlsError.SESSION_INITIALIZATION_FAILED:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, new TlsEvaluatedResult(ErrorId1,
                            EvaluatorResult.INCONCLUSIVE,
                            string.Format(intro,
                                $"we were unable to create a connection to the mail server. We will keep trying, so please check back later. Error description \"{tlsConnectionResult.ErrorDescription}\".")))
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
                case CurveGroup.Ffdhe2048:
                case CurveGroup.Ffdhe3072:
                case CurveGroup.Ffdhe4096:
                case CurveGroup.Ffdhe6144:
                case CurveGroup.Ffdhe8192:
                case CurveGroup.UnknownGroup2048:
                case CurveGroup.UnknownGroup3072:
                case CurveGroup.UnknownGroup4096:
                case CurveGroup.UnknownGroup6144:
                case CurveGroup.UnknownGroup8192:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, new Guid(), EvaluatorResult.PASS).ToTaskList();

                case CurveGroup.UnknownGroup1024:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId3, EvaluatorResult.WARNING,
                        string.Format(intro, $"the server selected an unknown 1024 bit group. {advice}")).ToTaskList();

                case CurveGroup.Java1024:
                case CurveGroup.Rfc2409_1024:
                case CurveGroup.Rfc5114_1024:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId4, EvaluatorResult.FAIL,
                            string.Format(intro,
                                $"the server selected {tlsConnectionResult.CurveGroup.GetEnumAsString()} which is an insecure 1024 bit (or less) group. {advice}"))
                        .ToTaskList();

                case CurveGroup.Unknown:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId5, EvaluatorResult.FAIL,
                            string.Format(intro,
                                $"the server selected an unknown group which is potentially insecure. {advice}"))
                        .ToTaskList();
            }

            return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId6, EvaluatorResult.INCONCLUSIVE,
                    string.Format(intro, "there was a problem and we are unable to provide additional information."))
                .ToTaskList();
        }

        public int SequenceNo => 6;
        public bool IsStopRule => false;
        public string Category => RuleCategory.Tls12;
    }
}