using System.Collections.Generic;
using Newtonsoft.Json;

namespace MailCheck.Mx.Contracts.SharedDomain
{
    public class TlsRecords
    {
        public TlsRecords(List<TlsRecord> records)
        {
            Records = records ?? new List<TlsRecord>();
        }

        [JsonConstructor]
        public TlsRecords(
            TlsRecord tls12AvailableWithBestCipherSuiteSelected,
            TlsRecord tls12AvailableWithBestCipherSuiteSelectedFromReverseList,
            TlsRecord tls12AvailableWithSha2HashFunctionSelected,
            TlsRecord tls12AvailableWithWeakCipherSuiteNotSelected,
            TlsRecord tls11AvailableWithBestCipherSuiteSelected,
            TlsRecord tls11AvailableWithWeakCipherSuiteNotSelected,
            TlsRecord tls10AvailableWithBestCipherSuiteSelected,
            TlsRecord tls10AvailableWithWeakCipherSuiteNotSelected,
            TlsRecord ssl3FailsWithBadCipherSuite,
            TlsRecord tlsSecureEllipticCurveSelected,
            TlsRecord tlsSecureDiffieHellmanGroupSelected,
            TlsRecord tlsWeakCipherSuitesRejected,
            TlsRecord tls12Available,
            TlsRecord tls11Available,
            TlsRecord tls10Available,
            TlsRecord tls13Available,
            TlsRecord tls13AvailableWithBestCipherSuiteSelected)
        {
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
            Tls12Available = tls12Available;
            Tls11Available = tls11Available;
            Tls10Available = tls10Available;
            Tls13Available = tls13Available;
            Tls13AvailableWithBestCipherSuiteSelected = tls13AvailableWithBestCipherSuiteSelected;

            Records = new List<TlsRecord>
            {
                tls12AvailableWithBestCipherSuiteSelected,
                tls12AvailableWithBestCipherSuiteSelectedFromReverseList,
                tls12AvailableWithSha2HashFunctionSelected,
                tls12AvailableWithWeakCipherSuiteNotSelected,
                tls11AvailableWithBestCipherSuiteSelected,
                tls11AvailableWithWeakCipherSuiteNotSelected,
                tls10AvailableWithBestCipherSuiteSelected,
                tls10AvailableWithWeakCipherSuiteNotSelected,
                ssl3FailsWithBadCipherSuite,
                tlsSecureEllipticCurveSelected,
                tlsSecureDiffieHellmanGroupSelected,
                tlsWeakCipherSuitesRejected,
                tls12Available,
                tls11Available,
                tls10Available,
                tls13Available,
                tls13AvailableWithBestCipherSuiteSelected
            };
        }
        public TlsRecord Tls12AvailableWithBestCipherSuiteSelected { get; }
        public TlsRecord Tls12AvailableWithBestCipherSuiteSelectedFromReverseList { get; }
        public TlsRecord Tls12AvailableWithSha2HashFunctionSelected { get; }
        public TlsRecord Tls12AvailableWithWeakCipherSuiteNotSelected { get; }
        public TlsRecord Tls11AvailableWithBestCipherSuiteSelected { get; }
        public TlsRecord Tls11AvailableWithWeakCipherSuiteNotSelected { get; }
        public TlsRecord Tls10AvailableWithBestCipherSuiteSelected { get; }
        public TlsRecord Tls10AvailableWithWeakCipherSuiteNotSelected { get; }
        public TlsRecord Ssl3FailsWithBadCipherSuite { get; }
        public TlsRecord TlsSecureEllipticCurveSelected { get; }
        public TlsRecord TlsSecureDiffieHellmanGroupSelected { get; }
        public TlsRecord TlsWeakCipherSuitesRejected { get; }
        public TlsRecord Tls12Available { get; }
        public TlsRecord Tls11Available { get; }
        public TlsRecord Tls10Available { get; }
        public TlsRecord Tls13Available { get; }
        public TlsRecord Tls13AvailableWithBestCipherSuiteSelected { get; }
        public List<TlsRecord> Records { get; set; }
    }
}
