using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Tester;
using MailCheck.Mx.TlsEvaluator.Util;

namespace MailCheck.Mx.TlsEvaluator.Rules.TlsEvaluation.Tls12
{
    public class Tls12AvailableWithSha2HashFunctionSelected : IRule<TlsTestResults, RuleTypedTlsEvaluationResult>
    {
        private readonly string advice = "Cipher suites with SHA-2 should be selected when presented by the client.";

        private readonly string intro =
            "When testing TLS 1.2 to ensure the most secure SHA hash function is selected {0}";

        public TlsTestType Type => TlsTestType.Tls12AvailableWithSha2HashFunctionSelected;

        public Guid ErrorId1 => Guid.Parse("632353AD-3449-4376-98D7-D7263415BA55");
        public Guid ErrorId2 => Guid.Parse("73170173-DB48-4C11-8417-4F1795A66C5B");
        public Guid ErrorId3 => Guid.Parse("A5614A2F-45CC-4C91-BC9B-0EE8E17E62F3");
        public Guid ErrorId4 => Guid.Parse("835F568B-598A-462F-B545-AFB783F9430A");
        public Guid ErrorId5 => Guid.Parse("6F7A8DF4-46AF-41DD-9DED-EF3E060638DC");
        public Guid ErrorId6 => Guid.Parse("66DCDC79-E7E3-4ECD-88BE-DD7BA23B6699");
        public Guid ErrorId7 => Guid.Parse("6BB15A73-5250-4DF9-B508-7CD6085E5DD4");

        public Task<List<RuleTypedTlsEvaluationResult>> Evaluate(TlsTestResults tlsTestConnectionResults)
        {
            BouncyCastleTlsTestResult tlsConnectionResult =
                tlsTestConnectionResults.Tls12AvailableWithSha2HashFunctionSelected;

            TlsTestType tlsTestType = TlsTestType.Tls12AvailableWithSha2HashFunctionSelected;

            if (tlsConnectionResult.TlsError == TlsError.HANDSHAKE_FAILURE ||
                tlsConnectionResult.TlsError == TlsError.INSUFFICIENT_SECURITY ||
                tlsConnectionResult.TlsError == TlsError.PROTOCOL_VERSION)
            {
                List<CipherSuite> tls12AvailableWithBestCipherSuiteSelectedPassingCipherSuites = new List<CipherSuite>
                {
                    CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384,
                    CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256,
                    CipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384,
                    CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256,
                    CipherSuite.TLS_DHE_RSA_WITH_AES_256_GCM_SHA384,
                    CipherSuite.TLS_DHE_RSA_WITH_AES_128_GCM_SHA256,
                    CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA384,
                    CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA256,
                    CipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA384,
                    CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA256
                };

                if (tlsTestConnectionResults.Tls12AvailableWithBestCipherSuiteSelected.CipherSuite != null &&
                    tls12AvailableWithBestCipherSuiteSelectedPassingCipherSuites.Contains(tlsTestConnectionResults
                        .Tls12AvailableWithBestCipherSuiteSelected.CipherSuite.Value))
                {
                    return new RuleTypedTlsEvaluationResult(tlsTestType, Guid.NewGuid(), EvaluatorResult.PASS)
                        .ToTaskList();
                }
            }

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
                    return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId2, EvaluatorResult.FAIL,
                            string.Format(intro,
                                $"the server responded with an error. Error description \"{tlsConnectionResult.ErrorDescription}\"."))
                        .ToTaskList();
            }

            string introWithCipherSuite = string.Format(intro,
                $"the server selected {tlsConnectionResult.CipherSuite.GetEnumAsString()}");

            switch (tlsConnectionResult.CipherSuite)
            {
                case CipherSuite.TLS_RSA_WITH_AES_256_GCM_SHA384:
                case CipherSuite.TLS_RSA_WITH_AES_128_GCM_SHA256:
                case CipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA256:
                case CipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA256:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, Guid.NewGuid(), EvaluatorResult.PASS)
                        .ToTaskList();

                case CipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA:
                case CipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA:
                case CipherSuite.TLS_DH_DSS_WITH_3DES_EDE_CBC_SHA:
                case CipherSuite.TLS_DH_RSA_WITH_3DES_EDE_CBC_SHA:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId3, EvaluatorResult.WARNING,
                        $"{introWithCipherSuite} which uses SHA-1. {advice}").ToTaskList();

                case CipherSuite.TLS_RSA_WITH_3DES_EDE_CBC_SHA:
                case CipherSuite.TLS_DHE_DSS_WITH_3DES_EDE_CBC_SHA:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId4, EvaluatorResult.WARNING,
                        $"{introWithCipherSuite} which uses 3DES and SHA-1. {advice}").ToTaskList();

                case CipherSuite.TLS_RSA_WITH_RC4_128_SHA:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId5, EvaluatorResult.WARNING,
                        $"{introWithCipherSuite} which uses RC4 and SHA-1. {advice}").ToTaskList();

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
                    return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId6, EvaluatorResult.FAIL,
                        $"{introWithCipherSuite} which is insecure. {advice}").ToTaskList();
            }

            return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId7, EvaluatorResult.INCONCLUSIVE,
                    string.Format(intro, "there was a problem and we are unable to provide additional information."))
                .ToTaskList();
        }

        public int SequenceNo => 4;
        public bool IsStopRule => false;
        public string Category => RuleCategory.Tls12;
    }
}