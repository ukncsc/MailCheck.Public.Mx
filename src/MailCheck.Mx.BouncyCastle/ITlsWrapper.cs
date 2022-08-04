using System.Collections.Generic;
using System.IO;
using MailCheck.Mx.Contracts.SharedDomain;
using CipherSuite = MailCheck.Mx.Contracts.SharedDomain.CipherSuite;

namespace MailCheck.Mx.BouncyCastle
{
    internal interface ITlsWrapper
    {
        BouncyCastleTlsTestResult ConnectWithResults(Stream stream, TlsVersion version, List<CipherSuite> cipherSuites);
    }
}
