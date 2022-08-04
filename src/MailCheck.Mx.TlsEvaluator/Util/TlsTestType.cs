namespace MailCheck.Mx.TlsEvaluator.Util
{
    public enum TlsTestType
    {
        Tls12AvailableWithBestCipherSuiteSelected = 1,
        Tls12AvailableWithBestCipherSuiteSelectedFromReverseList = 2,
        Tls12AvailableWithSha2HashFunctionSelected = 3,
        Tls12AvailableWithWeakCipherSuiteNotSelected = 4,
        Tls11AvailableWithBestCipherSuiteSelected = 5,
        Tls11AvailableWithWeakCipherSuiteNotSelected = 6,
        Tls10AvailableWithBestCipherSuiteSelected = 7,
        Tls10AvailableWithWeakCipherSuiteNotSelected = 8,
        Ssl3FailsWithBadCipherSuite = 9,
        TlsSecureEllipticCurveSelected = 10,
        TlsSecureDiffieHellmanGroupSelected = 11,
        TlsWeakCipherSuitesRejected = 12,
        Tls10Available = 13,
        Tls11Available = 14,
        Tls12Available = 15,
        Tls13Available = 16,
        Tls13AvailableWithBestCipherSuiteSelected = 17,
    }
}
