using System;
using System.Collections.Generic;
using MailCheck.Mx.Contracts.SharedDomain;

namespace MailCheck.Mx.Contracts.Tester
{
    public class TlsTestResults : Common.Messaging.Abstractions.Message
    {
        private TlsTestResults() : base(string.Empty) {}

        public TlsTestResults(string id,
            bool failed,
            bool hostNotFound,
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
            BouncyCastleTlsTestResult tls13AvailableWithBestCipherSuiteSelected,
            List<string> certificates, 
            List<SelectedCipherSuite> selectedCipherSuites = null) : base(id)
        {
            Failed = failed;
            HostNotFound = hostNotFound;
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
            SelectedCipherSuites = selectedCipherSuites;
            Certificates = certificates ?? new List<string>();
        }

        public bool Failed { get; }
        public bool HostNotFound { get; }
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
        public List<SelectedCipherSuite> SelectedCipherSuites { get; }
        public List<string> Certificates { get; }

        public static TlsTestResults CreateNullResults()
        {
            return new TlsTestResults();
        }
    }
}
