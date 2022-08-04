using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Tester;
using MailCheck.Mx.TlsEvaluator.Util;

namespace MailCheck.Mx.TlsEvaluator.Rules.TlsEvaluation.Tls13
{
    public class Tls13AvailableWithBestCipherSuiteSelected : IRule<TlsTestResults, RuleTypedTlsEvaluationResult>
    {
        public static readonly Guid SecureCipherSuiteId = new Guid("0f306036-d061-471b-9717-11cada1dd671");
        public static readonly Guid OtherCipherSuiteId = new Guid("9d2fcfe8-ef07-4d6d-909d-3a0a3d84c48a");
     
        public Task<List<RuleTypedTlsEvaluationResult>> Evaluate(TlsTestResults tlsTestConnectionResults)
        {
            BouncyCastleTlsTestResult tlsConnectionResult =
                tlsTestConnectionResults.Tls13AvailableWithBestCipherSuiteSelected;

            TlsTestType tlsTestType = TlsTestType.Tls13AvailableWithBestCipherSuiteSelected;

            switch (tlsConnectionResult.CipherSuite)
            {
                case CipherSuite.TLS_AES_256_GCM_SHA384:
                case CipherSuite.TLS_AES_128_GCM_SHA256:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, SecureCipherSuiteId, EvaluatorResult.PASS,
                            "This mailserver supports TLS 1.3 with recommended ciphersuites.")
                        .ToTaskList();

                default:
                    return new RuleTypedTlsEvaluationResult(tlsTestType, OtherCipherSuiteId, EvaluatorResult.INFORMATIONAL,
                        $"This mailserver does not support TLS 1.3 with the recommended ciphersuites.").ToTaskList();
            }
        }

        public int SequenceNo => 2;
        public bool IsStopRule => false;
        public string Category => RuleCategory.Tls13;
    }
}