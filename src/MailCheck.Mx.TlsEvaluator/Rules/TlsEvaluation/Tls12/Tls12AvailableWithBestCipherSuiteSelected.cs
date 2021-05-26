using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Tester;
using MailCheck.Mx.TlsEvaluator.Util;

namespace MailCheck.Mx.TlsEvaluator.Rules.TlsEvaluation.Tls12
{
    public class Tls12AvailableWithBestCipherSuiteSelected : IRule<TlsTestResults, RuleTypedTlsEvaluationResult>
    {
        private readonly string intro = "When testing TLS 1.2 with a range of cipher suites {0}";

        public Guid ErrorId1 => Guid.Parse("BA31306E-E604-4B71-BE82-4DB218197CA9");
        public Guid ErrorId2 => Guid.Parse("2C4BE40D-F9FB-4730-8EA7-65229832F758");
        public Guid ErrorId3 => Guid.Parse("05FCBA77-0BEF-4E4C-9C97-DE29A17202E0");
        public Guid ErrorId4 => Guid.Parse("72CE0914-35AF-4C50-9157-43D917714DA0");
        public Guid ErrorId5 => Guid.Parse("427D8611-7C6E-4372-88FE-2E850EEB9DC0");
        public Guid ErrorId6 => Guid.Parse("B9D2BC8E-759F-436B-B594-3025CC51F64C");
        public Guid ErrorId7 => Guid.Parse("8B06C3EC-8B5D-4EC6-B8C9-ADCA5DD14635");

        public Task<List<RuleTypedTlsEvaluationResult>> Evaluate(TlsTestResults tlsTestConnectionResults)
        {
            BouncyCastleTlsTestResult tlsConnectionResult =
                tlsTestConnectionResults.Tls12AvailableWithBestCipherSuiteSelected;

            string introWithCipherSuite = string.Format(intro,
                $"the server selected {tlsConnectionResult.CipherSuite.GetEnumAsString()}");

            TlsTestType tlsTestType = TlsTestType.Tls12AvailableWithBestCipherSuiteSelected;

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
                    return new RuleTypedTlsEvaluationResult(tlsTestType, Guid.NewGuid(), EvaluatorResult.PASS, 
                            "TLS 1.2 is available and a secure cipher suite was selected.")
                        .ToTaskList();

                case CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA:
                case CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA:
                case CipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA:
                case CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA:
                case CipherSuite.TLS_DHE_RSA_WITH_AES_256_CBC_SHA:
                case CipherSuite.TLS_DHE_RSA_WITH_AES_128_CBC_SHA:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId1, EvaluatorResult.WARNING,
                        $"{introWithCipherSuite} which uses SHA-1.").ToTaskList();

                case CipherSuite.TLS_RSA_WITH_AES_256_GCM_SHA384:
                case CipherSuite.TLS_RSA_WITH_AES_128_GCM_SHA256:
                case CipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA256:
                case CipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA256:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId2, EvaluatorResult.WARNING,
                        $"{introWithCipherSuite} which has no Perfect Forward Secrecy (PFS).").ToTaskList();

                case CipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA:
                case CipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId3, EvaluatorResult.WARNING,
                            $"{introWithCipherSuite} which has no Perfect Forward Secrecy (PFS) and uses SHA-1.")
                        .ToTaskList();

                case CipherSuite.TLS_RSA_WITH_3DES_EDE_CBC_SHA:
                case CipherSuite.TLS_DHE_DSS_WITH_3DES_EDE_CBC_SHA:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId4, EvaluatorResult.WARNING,
                            $"{introWithCipherSuite} which has no Perfect Forward Secrecy (PFS) and uses 3DES and SHA-1.")
                        .ToTaskList();

                case CipherSuite.TLS_RSA_WITH_RC4_128_SHA:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId5, EvaluatorResult.WARNING,
                            $"{introWithCipherSuite} which has no Perfect Forward Secrecy (PFS) and uses RC4 and SHA-1.")
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
                    return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId6, EvaluatorResult.FAIL,
                        $"{introWithCipherSuite} which is insecure.").ToTaskList();
            }

            return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId7, EvaluatorResult.INCONCLUSIVE,
                    string.Format(intro, "there was a problem and we are unable to provide additional information."))
                .ToTaskList();
        }

        public int SequenceNo => 2;
        public bool IsStopRule => false;
        public string Category => RuleCategory.Tls12;
    }
}