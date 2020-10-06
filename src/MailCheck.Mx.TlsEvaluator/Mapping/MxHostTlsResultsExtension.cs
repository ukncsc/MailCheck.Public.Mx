using System;
using System.Collections.Generic;
using System.Text;
using MailCheck.Mx.Contracts.Tester;
using MailCheck.Mx.Contracts.TlsEvaluator;

namespace MailCheck.Mx.TlsEvaluator.Mapping
{
    public static class MxHostTlsResultsExtension
    {
        public static TlsResults ToTlsResult(this TlsTestResults tlsTestResults)
        {
            return new TlsResults(tlsTestResults.Failed,
                tlsTestResults.Tls12AvailableWithBestCipherSuiteSelected,
                tlsTestResults.Tls12AvailableWithBestCipherSuiteSelectedFromReverseList,
                tlsTestResults.Tls12AvailableWithSha2HashFunctionSelected,
                tlsTestResults.Tls10AvailableWithWeakCipherSuiteNotSelected,
                tlsTestResults.Tls11AvailableWithBestCipherSuiteSelected,
                tlsTestResults.Tls11AvailableWithWeakCipherSuiteNotSelected,
                tlsTestResults.Tls10AvailableWithBestCipherSuiteSelected,
                tlsTestResults.Tls10AvailableWithWeakCipherSuiteNotSelected,
                tlsTestResults.Ssl3FailsWithBadCipherSuite,
                tlsTestResults.TlsSecureEllipticCurveSelected,
                tlsTestResults.TlsSecureDiffieHellmanGroupSelected,
                tlsTestResults.TlsWeakCipherSuitesRejected);
        }
    }
}
