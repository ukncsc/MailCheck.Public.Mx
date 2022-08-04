using System;
using MailCheck.Mx.Contracts.SharedDomain;

namespace MailCheck.Mx.Contracts.TlsEvaluator
{
    public class TlsResults
    {
        public TlsResults(
            bool failed,
            BouncyCastleTlsTestResult tls12AvailableWithBestCipherSuiteSelected,
            BouncyCastleTlsTestResult tls12AvailableWithBestCipherSuiteSelectedFromReverseList,
            BouncyCastleTlsTestResult tls12AvailableWithSha2HashFunctionSelected,
            BouncyCastleTlsTestResult tls12AvailableWithWeakCipherSuiteNotSelected,
            BouncyCastleTlsTestResult tls11AvailableWithBestCipherSuiteSelected,
            BouncyCastleTlsTestResult tls11AvailableWithWeakCipherSuiteNotSelected,
            BouncyCastleTlsTestResult tls10AvailableWithBestCipherSuiteSelected,
            BouncyCastleTlsTestResult tls10AvailableWithWeakCipherSuiteNotSelected,
            BouncyCastleTlsTestResult ssl3FailsWithBadCipherSuite,
            BouncyCastleTlsTestResult tlsSecureEllipticCurveSelected,
            BouncyCastleTlsTestResult tlsSecureDiffieHellmanGroupSelected,
            BouncyCastleTlsTestResult tlsWeakCipherSuitesRejected,
            BouncyCastleTlsTestResult tls13AvailableWithBestCipherSuiteSelected)
        {
            Failed = failed;
            Tls12AvailableWithBestCipherSuiteSelected = tls12AvailableWithBestCipherSuiteSelected;
            Tls12AvailableWithBestCipherSuiteSelectedFromReverseList =
                tls12AvailableWithBestCipherSuiteSelectedFromReverseList;
            Tls12AvailableWithSha2HashFunctionSelected = tls12AvailableWithSha2HashFunctionSelected;
            Tls12AvailableWithWeakCipherSuiteNotSelected = tls12AvailableWithWeakCipherSuiteNotSelected;
            Tls11AvailableWithBestCipherSuiteSelected = tls11AvailableWithBestCipherSuiteSelected;
            Tls11AvailableWithWeakCipherSuiteNotSelected = tls11AvailableWithWeakCipherSuiteNotSelected;
            Tls10AvailableWithBestCipherSuiteSelected = tls10AvailableWithBestCipherSuiteSelected;
            Tls10AvailableWithWeakCipherSuiteNotSelected = tls10AvailableWithWeakCipherSuiteNotSelected;
            Ssl3FailsWithBadCipherSuite = ssl3FailsWithBadCipherSuite;
            TlsSecureEllipticCurveSelected = tlsSecureEllipticCurveSelected;
            TlsSecureDiffieHellmanGroupSelected = tlsSecureDiffieHellmanGroupSelected;
            TlsWeakCipherSuitesRejected = tlsWeakCipherSuitesRejected;
            Tls13AvailableWithBestCipherSuiteSelected = tls13AvailableWithBestCipherSuiteSelected;
        }

        public bool Failed { get; }
        public BouncyCastleTlsTestResult Tls12AvailableWithBestCipherSuiteSelected { get; }
        public BouncyCastleTlsTestResult Tls12AvailableWithBestCipherSuiteSelectedFromReverseList { get; }
        public BouncyCastleTlsTestResult Tls12AvailableWithSha2HashFunctionSelected { get; }
        public BouncyCastleTlsTestResult Tls12AvailableWithWeakCipherSuiteNotSelected { get; }
        public BouncyCastleTlsTestResult Tls11AvailableWithBestCipherSuiteSelected { get; }
        public BouncyCastleTlsTestResult Tls11AvailableWithWeakCipherSuiteNotSelected { get; }
        public BouncyCastleTlsTestResult Tls10AvailableWithBestCipherSuiteSelected { get; }
        public BouncyCastleTlsTestResult Tls10AvailableWithWeakCipherSuiteNotSelected { get; }
        public BouncyCastleTlsTestResult Ssl3FailsWithBadCipherSuite { get; }
        [Obsolete]
        public BouncyCastleTlsTestResult TlsSecureEllipticCurveSelected { get; }
        public BouncyCastleTlsTestResult TlsSecureDiffieHellmanGroupSelected { get; }
        public BouncyCastleTlsTestResult TlsWeakCipherSuitesRejected { get; }
        public BouncyCastleTlsTestResult Tls13AvailableWithBestCipherSuiteSelected { get; }
    }
}
