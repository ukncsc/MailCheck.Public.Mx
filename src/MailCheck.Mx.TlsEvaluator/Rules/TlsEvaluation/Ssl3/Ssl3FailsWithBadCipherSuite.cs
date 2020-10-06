using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Tester;
using MailCheck.Mx.TlsEvaluator.Util;

namespace MailCheck.Mx.TlsEvaluator.Rules.TlsEvaluation.Ssl3
{
    public class Ssl3FailsWithBadCipherSuite : IRule<TlsTestResults, RuleTypedTlsEvaluationResult>
    {
        private readonly string advice = "SSL 3.0 is an insecure protocol and should be not supported.";
        private readonly string intro = "When testing SSL 3.0 with a range of cipher suites {0}";

        public Guid ErrorId1 => Guid.Parse("7C489522-A16C-41C8-9CF5-6E0598542977");
        public Guid ErrorId2 => Guid.Parse("42483BB1-F114-4998-BBFD-2C431C964D1D");
        public Guid ErrorId3 => Guid.Parse("336CB29E-C5E5-449C-A929-BA7A18A5CD0C");
        public Guid ErrorId4 => Guid.Parse("66F24799-3F4C-42DD-A89C-C2DCE2E95A08");
        public Guid ErrorId5 => Guid.Parse("AD10B670-E093-4639-97FA-4ECC7815391A");

        public Task<List<RuleTypedTlsEvaluationResult>> Evaluate(TlsTestResults tlsTestConnectionResults)
        {
            BouncyCastleTlsTestResult tlsConnectionResult = tlsTestConnectionResults.Ssl3FailsWithBadCipherSuite;

            TlsTestType tlsTestType = TlsTestType.Ssl3FailsWithBadCipherSuite;

            switch (tlsConnectionResult.TlsError)
            {
                case TlsError.HANDSHAKE_FAILURE:
                case TlsError.PROTOCOL_VERSION:
                case TlsError.INSUFFICIENT_SECURITY:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, Guid.NewGuid(), EvaluatorResult.PASS).ToTaskList();

                case TlsError.TCP_CONNECTION_FAILED:
                case TlsError.SESSION_INITIALIZATION_FAILED:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId1, EvaluatorResult.INCONCLUSIVE, string.Format(intro, $"we were unable to create a connection to the mail server. We will keep trying, so please check back later. Error description \"{tlsConnectionResult.ErrorDescription}\".")).ToTaskList();

                case null:
                    break;

                default:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId2, EvaluatorResult.INCONCLUSIVE, string.Format(intro, $"the server responded with an error. Error description \"{tlsConnectionResult.ErrorDescription}\".")).ToTaskList();
            }

            string introWithCipherSuite = string.Format(intro,
                $"the server accepted the connection and selected {tlsConnectionResult.CipherSuite.GetEnumAsString()}");

            switch (tlsConnectionResult.CipherSuite)
            {
                case CipherSuite.TLS_RSA_WITH_RC4_128_SHA:
                case CipherSuite.TLS_DH_DSS_WITH_3DES_EDE_CBC_SHA:
                case CipherSuite.TLS_DH_RSA_WITH_3DES_EDE_CBC_SHA:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId3, EvaluatorResult.WARNING, $"{introWithCipherSuite}. {advice}").ToTaskList();

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
                    return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId4, EvaluatorResult.FAIL, $"{introWithCipherSuite} which is insecure. {advice}").ToTaskList();
            }

            return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId5, EvaluatorResult.INCONCLUSIVE, string.Format(intro, "there was a problem and we are unable to provide additional information.")).ToTaskList();
        }

        public int SequenceNo => 1;
        public bool IsStopRule => false;
        public string Category => RuleCategory.Ssl3;
    }
}