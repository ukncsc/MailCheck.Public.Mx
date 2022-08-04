using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Tester;
using MailCheck.Mx.TlsEvaluator.Util;

namespace MailCheck.Mx.TlsEvaluator.Rules.TlsEvaluation.Tls10
{
    public class Tls10AvailableWithBestCipherSuiteSelected : IRule<TlsTestResults, RuleTypedTlsEvaluationResult>
    {
        private readonly string advice =
            "Cipher suites with Perfect Forward Secrecy should be selected when presented by the client.";

        private readonly string intro = "When testing TLS 1.0 with a range of cipher suites {0}";

        public Guid ErrorId1 => Guid.Parse("F0999C05-FF73-4308-9C85-32D7E08DC086");
        public Guid ErrorId2 => Guid.Parse("1DF00322-1446-4C38-A708-57DD263F5ACF");
        public Guid ErrorId3 => Guid.Parse("0B85B011-0C25-4693-82B7-22F937331F4E");
        public Guid ErrorId4 => Guid.Parse("625BF861-801A-4562-9732-5B87F76CE169");
        public Guid ErrorId5 => Guid.Parse("5EF90F62-B79C-435A-9CDB-9255D492B445");

        public Task<List<RuleTypedTlsEvaluationResult>> Evaluate(TlsTestResults tlsTestConnectionResults)
        {
            BouncyCastleTlsTestResult tlsConnectionResult =
                tlsTestConnectionResults.Tls10AvailableWithBestCipherSuiteSelected;

            string introWithCipherSuite = string.Format(intro,
                $"the server selected {tlsConnectionResult.CipherSuite.GetEnumAsString()}");

            TlsTestType tlsTestType = TlsTestType.Tls10AvailableWithBestCipherSuiteSelected;

            switch (tlsConnectionResult.CipherSuite)
            {
                case CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA:
                case CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA:
                case CipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA:
                case CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA:
                case CipherSuite.TLS_DHE_RSA_WITH_AES_256_CBC_SHA:
                case CipherSuite.TLS_DHE_RSA_WITH_AES_128_CBC_SHA:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, Guid.NewGuid(), EvaluatorResult.INFORMATIONAL,
                            "TLS 1.0 is available and a secure cipher suite was selected.")
                        .ToTaskList();

                case CipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA:
                case CipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId1, EvaluatorResult.WARNING,
                        $"{introWithCipherSuite} which has no Perfect Forward Secrecy (PFS). {advice}").ToTaskList();

                case CipherSuite.TLS_RSA_WITH_3DES_EDE_CBC_SHA:
                case CipherSuite.TLS_DH_DSS_WITH_3DES_EDE_CBC_SHA:
                case CipherSuite.TLS_DH_RSA_WITH_3DES_EDE_CBC_SHA:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId2, EvaluatorResult.WARNING,
                            $"{introWithCipherSuite} which has no Perfect Forward Secrecy (PFS) and uses 3DES. {advice}")
                        .ToTaskList();

                case CipherSuite.TLS_RSA_WITH_RC4_128_SHA:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId3, EvaluatorResult.WARNING,
                            $"{introWithCipherSuite} which has no Perfect Forward Secrecy (PFS) and uses RC4. {advice}")
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
                        $"{introWithCipherSuite} which is insecure. {advice}").ToTaskList();
            }

            return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId5, EvaluatorResult.INCONCLUSIVE,
                    string.Format(intro, "there was a problem and we are unable to provide additional information."))
                .ToTaskList();
        }

        public int SequenceNo => 2;
        public bool IsStopRule => false;
        public string Category => RuleCategory.Tls10;
    }
}