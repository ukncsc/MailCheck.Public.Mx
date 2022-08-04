using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Tester;
using MailCheck.Mx.TlsEvaluator.Util;

namespace MailCheck.Mx.TlsEvaluator.Rules.TlsEvaluation.Tls11
{
    public class Tls11AvailableWithWeakCipherSuiteNotSelected : IRule<TlsTestResults, RuleTypedTlsEvaluationResult>
    {
        private readonly string intro = "When testing TLS 1.1 with a range of weak cipher suites {0}";

        public Guid ErrorId1 => Guid.Parse("0AAED8A7-8E9B-40B3-A4B1-24240BFB60BE");
        public Guid ErrorId2 => Guid.Parse("6D51DF7F-F63D-446D-9053-E3828894BF53");
        public Guid ErrorId3 => Guid.Parse("0D3D7DD6-A622-4C59-A0E8-DC427D35BD90");
        public Guid ErrorId4 => Guid.Parse("55459947-1812-4353-83B4-3CBD00A2B602");
        public Guid ErrorId5 => Guid.Parse("B9441CC6-6B56-4750-8FC5-53604E90FBFA");

        public Task<List<RuleTypedTlsEvaluationResult>> Evaluate(TlsTestResults tlsTestConnectionResults)
        {
            BouncyCastleTlsTestResult tlsConnectionResult = tlsTestConnectionResults.Tls11AvailableWithWeakCipherSuiteNotSelected;
            BouncyCastleTlsTestResult tls12AvailableWithBestCipherSuiteSelectedResult = tlsTestConnectionResults.Tls12AvailableWithBestCipherSuiteSelected;

            TlsTestType tlsTestType = TlsTestType.Tls11AvailableWithWeakCipherSuiteNotSelected;

            switch (tlsConnectionResult.TlsError)
            {
                case TlsError.HANDSHAKE_FAILURE:
                case TlsError.PROTOCOL_VERSION:
                case TlsError.INSUFFICIENT_SECURITY:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, Guid.NewGuid(), EvaluatorResult.INFORMATIONAL).ToTaskList();

                case TlsError.TCP_CONNECTION_FAILED:
                case TlsError.SESSION_INITIALIZATION_FAILED:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId1, EvaluatorResult.INCONCLUSIVE,
                        string.Format(intro, $"we were unable to create a connection to the mail server. We will keep trying, so please check back later. Error description \"{tlsConnectionResult.ErrorDescription}\".")).ToTaskList();

                case null:
                    break;

                default:
                    return tls12AvailableWithBestCipherSuiteSelectedResult.TlsError == null
                        ? new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId2,EvaluatorResult.WARNING,
                            string.Format(intro, $"the server responded with an error. This may be because you do not support TLS 1.1. Error description \"{tlsConnectionResult.ErrorDescription}\".")).ToTaskList()
                        : new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId3, EvaluatorResult.INCONCLUSIVE,
                            string.Format(intro, $"the server responded with an error. Error description \"{tlsConnectionResult.ErrorDescription}\".")).ToTaskList();
            }

            switch (tlsConnectionResult.CipherSuite)
            {
                case CipherSuite.TLS_RSA_WITH_3DES_EDE_CBC_SHA:
                case CipherSuite.TLS_RSA_WITH_RC4_128_SHA:
                case CipherSuite.TLS_DH_DSS_WITH_3DES_EDE_CBC_SHA:
                case CipherSuite.TLS_DH_RSA_WITH_3DES_EDE_CBC_SHA:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, Guid.NewGuid(), EvaluatorResult.INFORMATIONAL).ToTaskList();

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
                    return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId4, EvaluatorResult.FAIL, string.Format(intro, $"the server selected {tlsConnectionResult.CipherSuite.GetEnumAsString()} which is insecure")).ToTaskList();
            }

            return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId5, EvaluatorResult.INCONCLUSIVE, string.Format(intro, "there was a problem and we are unable to provide additional information.")).ToTaskList();
        }

        public int SequenceNo => 3;
        public bool IsStopRule => false;
        public string Category => RuleCategory.Tls11;
    }
}
