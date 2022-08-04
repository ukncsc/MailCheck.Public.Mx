using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MailCheck.Mx.Contracts.SharedDomain;

namespace MailCheck.Mx.Contracts.Tester
{
    public static class TlsTestResultsExtensions
    {
        /// <summary>
        /// Enumerates all test results including nulls
        /// </summary>
        /// <param name="testResults"></param>
        /// <returns></returns>
        public static IEnumerable<BouncyCastleTlsTestResult> EnumerateAllResults(this TlsTestResults testResults)
        {
            if (testResults == null) yield break;

            yield return testResults.Tls12AvailableWithBestCipherSuiteSelected;
            yield return testResults.Tls12AvailableWithBestCipherSuiteSelectedFromReverseList;
            yield return testResults.Tls12AvailableWithSha2HashFunctionSelected;
            yield return testResults.Tls12AvailableWithWeakCipherSuiteNotSelected;
            yield return testResults.Tls11AvailableWithBestCipherSuiteSelected;
            yield return testResults.Tls11AvailableWithWeakCipherSuiteNotSelected;
            yield return testResults.Tls10AvailableWithBestCipherSuiteSelected;
            yield return testResults.Tls10AvailableWithWeakCipherSuiteNotSelected;
            yield return testResults.Ssl3FailsWithBadCipherSuite;
            yield return testResults.TlsSecureEllipticCurveSelected;
            yield return testResults.TlsSecureDiffieHellmanGroupSelected;
            yield return testResults.TlsWeakCipherSuitesRejected;
            yield return testResults.Tls13AvailableWithBestCipherSuiteSelected;
        }

        /// <summary>
        /// Enumerates test results excluding nulls
        /// </summary>
        /// <param name="testResults"></param>
        /// <returns></returns>
        public static IEnumerable<BouncyCastleTlsTestResult> EnumerateResults(this TlsTestResults testResults)
        {
            return testResults.EnumerateAllResults().Where(r => r != null);
        }
    }
}
