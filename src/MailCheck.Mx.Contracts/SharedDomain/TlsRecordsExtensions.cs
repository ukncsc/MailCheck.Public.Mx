using System.Collections.Generic;
using System.Linq;

namespace MailCheck.Mx.Contracts.SharedDomain
{
    public static class TlsRecordsExtensions
    {
        /// <summary>
        /// Enumerates non-null records
        /// </summary>
        /// <param name="tlsRecords"></param>
        /// <returns></returns>
        public static IEnumerable<TlsRecord> EnumerateRecords(this TlsRecords tlsRecords)
        {
            return tlsRecords.EnumerateAllRecords().Where(r => r != null);
        }

        /// <summary>
        /// Enumerates all records including nulls
        /// </summary>
        /// <param name="tlsRecords"></param>
        /// <returns></returns>
        public static IEnumerable<TlsRecord> EnumerateAllRecords(this TlsRecords tlsRecords)
        {
            if (tlsRecords == null) yield break;

            yield return tlsRecords.Tls12AvailableWithBestCipherSuiteSelected;
            yield return tlsRecords.Tls12AvailableWithBestCipherSuiteSelectedFromReverseList;
            yield return tlsRecords.Tls12AvailableWithSha2HashFunctionSelected;
            yield return tlsRecords.Tls12AvailableWithWeakCipherSuiteNotSelected;
            yield return tlsRecords.Tls11AvailableWithBestCipherSuiteSelected;
            yield return tlsRecords.Tls11AvailableWithWeakCipherSuiteNotSelected;
            yield return tlsRecords.Tls10AvailableWithBestCipherSuiteSelected;
            yield return tlsRecords.Tls10AvailableWithWeakCipherSuiteNotSelected;
            yield return tlsRecords.Ssl3FailsWithBadCipherSuite;
            yield return tlsRecords.TlsSecureEllipticCurveSelected;
            yield return tlsRecords.TlsSecureDiffieHellmanGroupSelected;
            yield return tlsRecords.TlsWeakCipherSuitesRejected;
            yield return tlsRecords.Tls12Available;
            yield return tlsRecords.Tls11Available;
            yield return tlsRecords.Tls10Available;
            yield return tlsRecords.Tls13Available;
            yield return tlsRecords.Tls13AvailableWithBestCipherSuiteSelected;
        }
    }
}
