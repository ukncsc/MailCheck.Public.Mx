using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Tester;
using MailCheck.Mx.TlsEvaluator.Util;

namespace MailCheck.Mx.TlsEvaluator.Rules.TlsEvaluation.Tls10
{
    public class Tls10AvailableWithWeakCipherSuiteNotSelected : IRule<TlsTestResults, RuleTypedTlsEvaluationResult>
    {
        private readonly string intro = "When testing TLS 1.0 with a range of weak cipher suites {0}";
        
        public Guid ErrorId1 => Guid.Parse("A8105B57-AB46-4F02-9B69-6EB060324A2C");
        public Guid ErrorId2 => Guid.Parse("80A7D562-007F-46DB-88B2-AA0CFA795A4C");
        public Guid ErrorId3 => Guid.Parse("B7CAE40C-DC2B-45D6-B3C7-BDBFC53ECF74");
        public Guid ErrorId4 => Guid.Parse("16C163F4-EC2B-47B2-971A-E3245C7A1FD3");
        public Guid ErrorId5 => Guid.Parse("5263EE72-DAE4-43BE-A023-E33A8F64BCFE");

        public Task<List<RuleTypedTlsEvaluationResult>> Evaluate(TlsTestResults tlsTestConnectionResults)
        {
            BouncyCastleTlsTestResult tlsConnectionResult =
                tlsTestConnectionResults.Tls10AvailableWithWeakCipherSuiteNotSelected;
            BouncyCastleTlsTestResult tls12AvailableWithBestCipherSuiteSelectedResult =
                tlsTestConnectionResults.Tls12AvailableWithBestCipherSuiteSelected;

            TlsTestType tlsTestType = TlsTestType.Tls10AvailableWithWeakCipherSuiteNotSelected;

            switch (tlsConnectionResult.TlsError)
            {
                case TlsError.HANDSHAKE_FAILURE:
                case TlsError.PROTOCOL_VERSION:
                case TlsError.INSUFFICIENT_SECURITY:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, Guid.NewGuid(), EvaluatorResult.INFORMATIONAL)
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
                    return tls12AvailableWithBestCipherSuiteSelectedResult.TlsError == null
                        ? new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId2, EvaluatorResult.WARNING,
                                string.Format(intro,
                                    $"the server responded with an error. This may be because you do not support TLS 1.0. Error description \"{tlsConnectionResult.ErrorDescription}\"."))
                            .ToTaskList()
                        : new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId3, EvaluatorResult.INCONCLUSIVE,
                                string.Format(intro,
                                    $"the server responded with an error. Error description \"{tlsConnectionResult.ErrorDescription}\"."))
                            .ToTaskList();
            }

            string introWithCipherSuite =
                string.Format(intro, $"the server selected {tlsConnectionResult.CipherSuite.GetEnumAsString()}");

            switch (tlsConnectionResult.CipherSuite)
            {
                case CipherSuite.TLS_RSA_WITH_3DES_EDE_CBC_SHA:
                case CipherSuite.TLS_RSA_WITH_RC4_128_SHA:
                case CipherSuite.TLS_DH_DSS_WITH_3DES_EDE_CBC_SHA:
                case CipherSuite.TLS_DH_RSA_WITH_3DES_EDE_CBC_SHA:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, Guid.NewGuid(), EvaluatorResult.INFORMATIONAL)
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
                    return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId4, EvaluatorResult.FAIL,
                        $"{introWithCipherSuite} which is insecure.").ToTaskList();
            }

            return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId5, EvaluatorResult.INCONCLUSIVE,
                    string.Format(intro, "there was a problem and we are unable to provide additional information."))
                .ToTaskList();
        }

        public int SequenceNo => 3;
        public bool IsStopRule => false;
        public string Category => RuleCategory.Tls10;
    }
}
