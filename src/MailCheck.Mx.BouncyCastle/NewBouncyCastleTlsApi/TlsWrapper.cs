using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MailCheck.Mx.BouncyCastle.NewBouncyCastleTlsApi.Mapping;
using MailCheck.Mx.Contracts.SharedDomain;

namespace MailCheck.Mx.BouncyCastle.NewBouncyCastleTlsApi
{
    internal class TlsWrapper: ITlsWrapper
    {
        public BouncyCastleTlsTestResult ConnectWithResults(Stream stream, TlsVersion version, List<CipherSuite> cipherSuites)
        {
            var supportedTlsVersions = new[] { version.ToProtocolVersion() };
            int[] supportedCipherSuites = cipherSuites.Select(cipherSuite => (int)cipherSuite).ToArray();
            var tlsClient = new TestTlsClient(supportedTlsVersions, supportedCipherSuites);
            var tlsClientProtocol = new TestTlsClientProtocol(stream);

            try
            {
                return tlsClientProtocol.ConnectWithResults(tlsClient);
            }
            finally
            {
                tlsClientProtocol.Close();
            }
        }
    }
}
