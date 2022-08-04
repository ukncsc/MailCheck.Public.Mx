using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MailCheck.Mx.Contracts.SharedDomain;

namespace MailCheck.Mx.BouncyCastle.OldBouncyCastleTlsApi
{
    internal class TlsWrapper : ITlsWrapper
    {
        public BouncyCastleTlsTestResult ConnectWithResults(Stream stream, TlsVersion version, List<CipherSuite> cipherSuites)
        {
            var clientProtocol = new TestTlsClientProtocol(stream);
            var testSuiteTlsClient = new TestTlsClient(version, cipherSuites);
            try
            {
                return clientProtocol.ConnectWithResults(testSuiteTlsClient);
            }
            finally
            {
                clientProtocol.Close();
            }
        }
    }
}
