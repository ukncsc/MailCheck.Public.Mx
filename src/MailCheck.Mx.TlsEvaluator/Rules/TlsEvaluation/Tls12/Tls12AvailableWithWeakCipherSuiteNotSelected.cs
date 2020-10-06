using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Tester;
using MailCheck.Mx.TlsEvaluator.Util;

namespace MailCheck.Mx.TlsEvaluator.Rules.TlsEvaluation.Tls12
{
    public class Tls12AvailableWithWeakCipherSuiteNotSelected : IRule<TlsTestResults, RuleTypedTlsEvaluationResult>
    {
        private readonly string intro = "When testing TLS 1.2 with a range of weak cipher suites {0}";

        public TlsTestType Type => TlsTestType.Tls12AvailableWithWeakCipherSuiteNotSelected;

        public Guid ErrorId1 => Guid.Parse("0C14F8EA-F850-46D7-8831-AFEB9EC5D478");
        public Guid ErrorId2 => Guid.Parse("81A27EA1-9AEB-48A3-A547-2DF25830EA6A");
        public Guid ErrorId3 => Guid.Parse("C1C0CB30-90CF-4C8E-8356-F0234361F064");
        public Guid ErrorId4 => Guid.Parse("B654E058-C7C7-479E-B6D7-5785FA8C922D");

        public Task<List<RuleTypedTlsEvaluationResult>> Evaluate(TlsTestResults tlsTestConnectionResults)
        {
            BouncyCastleTlsTestResult tlsConnectionResult = tlsTestConnectionResults.Tls12AvailableWithWeakCipherSuiteNotSelected;

            TlsTestType tlsTestType = TlsTestType.Tls12AvailableWithWeakCipherSuiteNotSelected;

            switch (tlsConnectionResult.TlsError)
            {
                case TlsError.HANDSHAKE_FAILURE:
                case TlsError.PROTOCOL_VERSION:
                case TlsError.INSUFFICIENT_SECURITY:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, Guid.NewGuid(), EvaluatorResult.PASS).ToTaskList();

                case TlsError.TCP_CONNECTION_FAILED:
                case TlsError.SESSION_INITIALIZATION_FAILED:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId1, EvaluatorResult.INCONCLUSIVE,
                        string.Format(intro, $"we were unable to create a connection to the mail server. We will keep trying, so please check back later. Error description \"{tlsConnectionResult.ErrorDescription}\".")).ToTaskList();

                case null:
                    break;

                default:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId2, EvaluatorResult.INCONCLUSIVE,
                        string.Format(intro, $"the server responded with an error. Error description - {tlsConnectionResult.ErrorDescription}.")).ToTaskList();
            }

            switch (tlsConnectionResult.CipherSuite)
            {
                case CipherSuite.TLS_RSA_WITH_3DES_EDE_CBC_SHA:
                case CipherSuite.TLS_DHE_DSS_WITH_3DES_EDE_CBC_SHA:
                case CipherSuite.TLS_RSA_WITH_RC4_128_SHA:
                case CipherSuite.TLS_DH_DSS_WITH_3DES_EDE_CBC_SHA:
                case CipherSuite.TLS_DH_RSA_WITH_3DES_EDE_CBC_SHA:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, Guid.NewGuid(), EvaluatorResult.PASS).ToTaskList();

                case CipherSuite.TLS_NULL_WITH_NULL_NULL:
                case CipherSuite.TLS_RSA_WITH_NULL_MD5:
                case CipherSuite.TLS_RSA_WITH_RC4_128_MD5:
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
                    return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId3, EvaluatorResult.FAIL, string.Format(intro, $"the server selected {tlsConnectionResult.CipherSuite.GetEnumAsString()} which is insecure.")).ToTaskList();
            }

            return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId4, EvaluatorResult.INCONCLUSIVE, string.Format(intro, "there was a problem and we are unable to provide additional information.")).ToTaskList();
        }

        public int SequenceNo => 5;
        public bool IsStopRule => false;
        public string Category => RuleCategory.Tls12;
    }
}
