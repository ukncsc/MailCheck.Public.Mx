using System.Collections.Generic;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Tester;
using MailCheck.Mx.TlsEvaluator.Util;

namespace MailCheck.Mx.TlsEvaluator.Test
{
    public class TlsTestDataUtil
    {
        private static BouncyCastleTlsTestResult SetupConnectionResult(IDictionary<TlsTestType, BouncyCastleTlsTestResult> data, TlsTestType testType)
        {
            return data.ContainsKey(testType) 
                ? data[testType]
                : new BouncyCastleTlsTestResult(TlsError.BAD_CERTIFICATE, "Bad certificate found", null);
        }

        public static TlsTestResults CreateMxHostTlsResults(IDictionary<TlsTestType, BouncyCastleTlsTestResult> data)
        {
            return new TlsTestResults("abc.def.gov.uk", false, false,
                SetupConnectionResult(data, TlsTestType.Tls12AvailableWithBestCipherSuiteSelected),
                SetupConnectionResult(data, TlsTestType.Tls12AvailableWithBestCipherSuiteSelectedFromReverseList),
                SetupConnectionResult(data, TlsTestType.Tls12AvailableWithSha2HashFunctionSelected),
                SetupConnectionResult(data, TlsTestType.Tls12AvailableWithWeakCipherSuiteNotSelected),
                SetupConnectionResult(data, TlsTestType.Tls11AvailableWithBestCipherSuiteSelected),
                SetupConnectionResult(data, TlsTestType.Tls11AvailableWithWeakCipherSuiteNotSelected),
                SetupConnectionResult(data, TlsTestType.Tls10AvailableWithBestCipherSuiteSelected),
                SetupConnectionResult(data, TlsTestType.Tls10AvailableWithWeakCipherSuiteNotSelected),
                SetupConnectionResult(data, TlsTestType.Ssl3FailsWithBadCipherSuite),
                SetupConnectionResult(data, TlsTestType.TlsSecureEllipticCurveSelected),
                SetupConnectionResult(data, TlsTestType.TlsSecureDiffieHellmanGroupSelected),
                SetupConnectionResult(data, TlsTestType.TlsWeakCipherSuitesRejected), null);
        }

        public static TlsTestResults CreateMxHostTlsResults(TlsTestType testType, BouncyCastleTlsTestResult data)
        {
            return new TlsTestResults("abc.def.gov.uk", false, false, 
                SetupConnectionResult(new Dictionary<TlsTestType, BouncyCastleTlsTestResult> {{testType, data}},
                    TlsTestType.Tls12AvailableWithBestCipherSuiteSelected),
                SetupConnectionResult(new Dictionary<TlsTestType, BouncyCastleTlsTestResult> {{testType, data}},
                    TlsTestType.Tls12AvailableWithBestCipherSuiteSelectedFromReverseList),
                SetupConnectionResult(new Dictionary<TlsTestType, BouncyCastleTlsTestResult> {{testType, data}},
                    TlsTestType.Tls12AvailableWithSha2HashFunctionSelected),
                SetupConnectionResult(new Dictionary<TlsTestType, BouncyCastleTlsTestResult> {{testType, data}},
                    TlsTestType.Tls12AvailableWithWeakCipherSuiteNotSelected),
                SetupConnectionResult(new Dictionary<TlsTestType, BouncyCastleTlsTestResult> {{testType, data}},
                    TlsTestType.Tls11AvailableWithBestCipherSuiteSelected),
                SetupConnectionResult(new Dictionary<TlsTestType, BouncyCastleTlsTestResult> {{testType, data}},
                    TlsTestType.Tls11AvailableWithWeakCipherSuiteNotSelected),
                SetupConnectionResult(new Dictionary<TlsTestType, BouncyCastleTlsTestResult> {{testType, data}},
                    TlsTestType.Tls10AvailableWithBestCipherSuiteSelected),
                SetupConnectionResult(new Dictionary<TlsTestType, BouncyCastleTlsTestResult> {{testType, data}},
                    TlsTestType.Tls10AvailableWithWeakCipherSuiteNotSelected),
                SetupConnectionResult(new Dictionary<TlsTestType, BouncyCastleTlsTestResult> {{testType, data}},
                    TlsTestType.Ssl3FailsWithBadCipherSuite),
                SetupConnectionResult(new Dictionary<TlsTestType, BouncyCastleTlsTestResult> {{testType, data}},
                    TlsTestType.TlsSecureEllipticCurveSelected),
                SetupConnectionResult(new Dictionary<TlsTestType, BouncyCastleTlsTestResult> {{testType, data}},
                    TlsTestType.TlsSecureDiffieHellmanGroupSelected),
                SetupConnectionResult(new Dictionary<TlsTestType, BouncyCastleTlsTestResult> {{testType, data}},
                    TlsTestType.TlsWeakCipherSuitesRejected),
                null);
        }
    }
}
