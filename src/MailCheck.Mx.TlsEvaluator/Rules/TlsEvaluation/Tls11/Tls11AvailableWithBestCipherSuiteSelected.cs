using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Tester;
using MailCheck.Mx.TlsEvaluator.Util;

namespace MailCheck.Mx.TlsEvaluator.Rules.TlsEvaluation.Tls11
{
    public class Tls11AvailableWithBestCipherSuiteSelected : IRule<TlsTestResults, RuleTypedTlsEvaluationResult>
    {
        private readonly string advice =
            "Cipher suites with Perfect Forward Secrecy should be selected when presented by the client.";

        private readonly string intro = "When testing TLS 1.1 with a range of cipher suites {0}";

        public TlsTestType Type => TlsTestType.Tls11AvailableWithBestCipherSuiteSelected;

        public Guid ErrorId1 => Guid.Parse("31483F25-33D2-4F46-8115-E4E8E4D7DBCD");
        public Guid ErrorId2 => Guid.Parse("44345121-4942-4EEB-A164-C438BC964864");
        public Guid ErrorId3 => Guid.Parse("CDBD6E98-26B9-44E5-A877-66B584BC3DD1");
        public Guid ErrorId4 => Guid.Parse("D75983D3-5FF7-4FAF-AB4B-AC39A349CD72");
        public Guid ErrorId5 => Guid.Parse("7386247A-F942-4734-96B3-F1C7C2D3F970");

        public Task<List<RuleTypedTlsEvaluationResult>> Evaluate(TlsTestResults tlsTestConnectionResults)
        {
            BouncyCastleTlsTestResult tlsConnectionResult = tlsTestConnectionResults.Tls11AvailableWithBestCipherSuiteSelected;

            string introWithCipherSuite =
                string.Format(intro, $"the server selected {tlsConnectionResult.CipherSuite.GetEnumAsString()}");

            TlsTestType tlsTestType = TlsTestType.Tls11AvailableWithBestCipherSuiteSelected;
      
            switch (tlsConnectionResult.CipherSuite)
            {
                case CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA:
                case CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA:
                case CipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA:
                case CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA:
                case CipherSuite.TLS_DHE_RSA_WITH_AES_256_CBC_SHA:
                case CipherSuite.TLS_DHE_RSA_WITH_AES_128_CBC_SHA:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, Guid.NewGuid(), EvaluatorResult.INFORMATIONAL,
                            "TLS 1.1 is available and a secure cipher suite was selected.")
                        .ToTaskList();

                case CipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA:
                case CipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId1, EvaluatorResult.WARNING,
                        $"{introWithCipherSuite} which has no Perfect Forward Secrecy (PFS). {advice}").ToTaskList();

                case CipherSuite.TLS_RSA_WITH_3DES_EDE_CBC_SHA:
                case CipherSuite.TLS_DH_DSS_WITH_3DES_EDE_CBC_SHA:
                case CipherSuite.TLS_DH_RSA_WITH_3DES_EDE_CBC_SHA:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId2, EvaluatorResult.WARNING,
                        $"{introWithCipherSuite} which has no Perfect Forward Secrecy (PFS) and uses 3DES. {advice}").ToTaskList();

                case CipherSuite.TLS_RSA_WITH_RC4_128_SHA:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, ErrorId3, EvaluatorResult.WARNING,
                        $"{introWithCipherSuite} which has no Perfect Forward Secrecy (PFS) and uses RC4. {advice}").ToTaskList();

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
                string.Format(intro, "there was a problem and we are unable to provide additional information.")).ToTaskList();
        }

        public int SequenceNo => 2;
        public bool IsStopRule => false;
        public string Category => RuleCategory.Tls11;
    }
}