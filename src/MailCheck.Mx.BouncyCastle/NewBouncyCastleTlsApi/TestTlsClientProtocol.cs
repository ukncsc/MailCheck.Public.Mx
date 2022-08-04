using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using MailCheck.Mx.BouncyCastle.NewBouncyCastleTlsApi.Mapping;
using MailCheck.Mx.Contracts.SharedDomain;
using Org.BouncyCastle.Tls;
using CipherSuite = MailCheck.Mx.Contracts.SharedDomain.CipherSuite;

namespace MailCheck.Mx.BouncyCastle.NewBouncyCastleTlsApi
{
    internal class TestTlsClientProtocol : TlsClientProtocol
    {
        private TlsError? _tlsError;
        private string _errorMessage;

        public TestTlsClientProtocol(Stream stream) : base(stream)
        {
        }

        public BouncyCastleTlsTestResult ConnectWithResults(Org.BouncyCastle.Tls.TlsClient tlsClient)
        {
            try
            {
                Connect(tlsClient);

                TlsVersion version = Context.ServerVersion.ToTlsVersion();
                (CipherSuite cipherSuite, List<X509Certificate2> certificates) = Context.SecurityParameters.ToSharedDomain();

                return new BouncyCastleTlsTestResult(version, cipherSuite, null, null, _tlsError, _errorMessage, null, certificates);
            }
            catch (TlsFatalAlertReceived e)
            {
                _tlsError = (TlsError)e.AlertDescription;
                _errorMessage = e.Message;
                return new BouncyCastleTlsTestResult(_tlsError.Value, e.Message, null);
            }
            catch (TlsFatalAlert e)
            {
                _tlsError = (TlsError)e.AlertDescription;
                _errorMessage = e.Message;
                return new BouncyCastleTlsTestResult(_tlsError.Value, e.Message, null);
            }
            catch (Exception e)
            {
                _tlsError = TlsError.INTERNAL_ERROR;
                _errorMessage = e.Message;
                return new BouncyCastleTlsTestResult(_tlsError.Value, e.Message, null);
            }
            finally
            {
                base.CleanupHandshake();
            }
        }

        protected override void CleanupHandshake() { }
    }
}