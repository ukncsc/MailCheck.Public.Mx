using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Tester;
using MailCheck.Mx.TlsEvaluator.Util;

namespace MailCheck.Mx.TlsEvaluator.Rules.TlsEvaluation.Tls12
{
    public class
        Tls12AvailableWithBestCipherSuiteSelectedFromReverseList : IRule<TlsTestResults, RuleTypedTlsEvaluationResult>
    {
        private readonly string advice =
            "The server should choose the same cipher suite regardless of the order that they are presented by the client.";

        private readonly string intro = "When testing TLS 1.2 with a range of cipher suites in reverse order {0}";

        public TlsTestType Type => TlsTestType.Tls12AvailableWithBestCipherSuiteSelectedFromReverseList;

        public Guid ErrorId1 => Guid.Parse("4FA8A45A-D48E-4702-8698-7A30FF927EC3");
        public Guid ErrorId2 => Guid.Parse("F20F58A0-79CE-4921-92A7-033DA6CB4724");
        public Guid ErrorId3 => Guid.Parse("4D171A48-ED32-43EE-8E46-A2D501526A4A");
        public Guid ErrorId4 => Guid.Parse("08313CCC-9BBA-4C33-9FD7-0E37FEFD52ED");
        public Guid ErrorId5 => Guid.Parse("F71C8802-AD4B-4844-9429-315DC566E216");
        public Guid ErrorId6 => Guid.Parse("9F200BC1-BF50-4DF6-A34D-5278A82E2245");
        public Guid ErrorId7 => Guid.Parse("801B875E-4761-42C6-88D4-3A85860DCA5D");
        public Guid ErrorId8 => Guid.Parse("701BDF98-FE3D-41B7-B704-B32051696A57");
        public Guid ErrorId9 => Guid.Parse("3BF4D1C3-1A98-4D2D-A4A9-4C0F3BEF1FFA");

        public Task<List<RuleTypedTlsEvaluationResult>> Evaluate(TlsTestResults tlsTestConnectionResults)
        {
            BouncyCastleTlsTestResult tlsConnectionResult =
                tlsTestConnectionResults.Tls12AvailableWithBestCipherSuiteSelectedFromReverseList;

            CipherSuite? previousCipherSuite =
                tlsTestConnectionResults.Tls12AvailableWithBestCipherSuiteSelected.CipherSuite;

            TlsTestType tlsTestType = TlsTestType.Tls12AvailableWithBestCipherSuiteSelectedFromReverseList;

            switch (tlsConnectionResult.TlsError)
            {
                case TlsError.TCP_CONNECTION_FAILED:
                case TlsError.SESSION_INITIALIZATION_FAILED:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId1, EvaluatorResult.INCONCLUSIVE,
                            string.Format(intro,
                                $"we were unable to create a connection to the mail server. We will keep trying, so please check back later. Error description \"{tlsConnectionResult.ErrorDescription}\"."))
                        .ToTaskList();

                case null:
                    break;

                default:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId2, EvaluatorResult.WARNING,
                            string.Format(intro,
                                $"the server responded with an error. Error description - {tlsConnectionResult.ErrorDescription}. {advice}"))
                        .ToTaskList();
            }

            if (tlsConnectionResult.CipherSuite == previousCipherSuite)
            {
                return new RuleTypedTlsEvaluationResult(tlsTestType, Guid.NewGuid(), EvaluatorResult.PASS).ToTaskList();
            }

            string introWithCipherSuite = string.Format(intro,
                $"the server selected a different cipher suite ({tlsConnectionResult.CipherSuite.GetEnumAsString()})");

            switch (tlsConnectionResult.CipherSuite)
            {
                case CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384:
                case CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256:
                case CipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384:
                case CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256:
                case CipherSuite.TLS_DHE_RSA_WITH_AES_256_GCM_SHA384:
                case CipherSuite.TLS_DHE_RSA_WITH_AES_128_GCM_SHA256:
                case CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA384:
                case CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA256:
                case CipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA384:
                case CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA256:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, Guid.NewGuid(), EvaluatorResult.PASS)
                        .ToTaskList();

                case CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA:
                case CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA:
                case CipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA:
                case CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA:
                case CipherSuite.TLS_DHE_RSA_WITH_AES_256_CBC_SHA:
                case CipherSuite.TLS_DHE_RSA_WITH_AES_128_CBC_SHA:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId3, EvaluatorResult.INFORMATIONAL,
                        $"{introWithCipherSuite} which uses SHA-1. {advice}").ToTaskList();

                case CipherSuite.TLS_RSA_WITH_AES_256_GCM_SHA384:
                case CipherSuite.TLS_RSA_WITH_AES_128_GCM_SHA256:
                case CipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA256:
                case CipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA256:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId4, EvaluatorResult.INFORMATIONAL,
                        $"{introWithCipherSuite} which has no Perfect Forward Secrecy (PFS). {advice}").ToTaskList();

                case CipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA:
                case CipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId5, EvaluatorResult.INFORMATIONAL,
                            $"{introWithCipherSuite} which has no Perfect Forward Secrecy (PFS) and uses SHA-1. {advice}")
                        .ToTaskList();

                case CipherSuite.TLS_RSA_WITH_3DES_EDE_CBC_SHA:
                case CipherSuite.TLS_DHE_DSS_WITH_3DES_EDE_CBC_SHA:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId6, EvaluatorResult.INFORMATIONAL,
                            $"{introWithCipherSuite} which has no Perfect Forward Secrecy (PFS) and uses 3DES and SHA-1. {advice}")
                        .ToTaskList();

                case CipherSuite.TLS_RSA_WITH_RC4_128_SHA:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId7, EvaluatorResult.INFORMATIONAL,
                            $"{introWithCipherSuite} which has no Perfect Forward Secrecy (PFS) and uses RC4 and SHA-1. {advice}")
                        .ToTaskList();

                case CipherSuite.TLS_RSA_WITH_RC4_128_MD5:
                case CipherSuite.TLS_NULL_WITH_NULL_NULL:
                case CipherSuite.TLS_RSA_WITH_NULL_MD5:
                case CipherSuite.TLS_RSA_WITH_NULL_SHA:
                case CipherSuite.TLS_RSA_EXPORT_WITH_RC4_40_MD5:
                case CipherSuite.TLS_RSA_EXPORT_WITH_RC2_CBC_40_MD5:
                case CipherSuite.TLS_RSA_EXPORT_WITH_DES40_CBC_SHA:
                case CipherSuite.TLS_RSA_WITH_DES_CBC_SHA:
                case CipherSuite.TLS_DH_DSS_EXPORT_WITH_DES40_CBC_SHA:
                case CipherSuite.TLS_DH_DSS_WITH_DES_CBC_SHA:
                case CipherSuite.TLS_DH_RSA_EXPORT_WITH_DES40_CBC_SHA:
                case CipherSuite.TLS_DH_RSA_WITH_DES_CBC_SHA:
                case CipherSuite.TLS_DHE_DSS_EXPORT_WITH_DES40_CBC_SHA:
                case CipherSuite.TLS_DHE_DSS_WITH_DES_CBC_SHA:
                case CipherSuite.TLS_DHE_RSA_EXPORT_WITH_DES40_CBC_SHA:
                case CipherSuite.TLS_DHE_RSA_WITH_DES_CBC_SHA:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId8, EvaluatorResult.FAIL,
                        $"{introWithCipherSuite} which is insecure. {advice}").ToTaskList();
            }

            return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId9, EvaluatorResult.INCONCLUSIVE,
                    string.Format(intro, "there was a problem and we are unable to provide additional information."))
                .ToTaskList();
        }

        public int SequenceNo => 3;
        public bool IsStopRule => false;
        public string Category => RuleCategory.Tls12;
    }
}
