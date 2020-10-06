using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using MailCheck.Mx.BouncyCastle.Config;
using MailCheck.Mx.Contracts.SharedDomain;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Security;
using CipherSuite = MailCheck.Mx.Contracts.SharedDomain.CipherSuite;

namespace MailCheck.Mx.BouncyCastle
{
    public interface ITlsClient
    {
        Task Connect(string host, int port);
        Task<BouncyCastleTlsTestResult> Connect(string host, int port, TlsVersion version, List<CipherSuite> cipherSuites);
        NetworkStream GetStream();
        void Disconnect();
    }

    public class TlsClient : ITlsClient
    {
        private readonly TimeSpan _timeOut;

        private readonly ILogger _log;
        private TcpClient _tcpClient;

        public TlsClient(ILogger<TlsClient> log,
            IBouncyCastleClientConfig config)
        {
            _log = log;
            _timeOut = config.TlsConnectionTimeOut;
        }

        public TlsClient()
        {
            _timeOut = TimeSpan.FromSeconds(300); //5 minutes specified by RFC
        }

        public async Task Connect(string host, int port)
        {
            _tcpClient = new TcpClient();

            await _tcpClient.ConnectAsync(host, port).ConfigureAwait(false);

            StartTlsResult sessionInitialized = await TryInitializeSession(_tcpClient.GetStream()).ConfigureAwait(false);

            if (!sessionInitialized.Success)
            {
                throw new Exception("Failed to initialize session.");
            }

            TlsClientProtocol clientProtocol = new TlsClientProtocol(_tcpClient.GetStream(), SecureRandom.GetInstance("SHA256PRNG"));

            clientProtocol.Connect(new BasicTlsClient());
        }

        public async Task<BouncyCastleTlsTestResult> Connect(string host, int port, TlsVersion version, List<CipherSuite> cipherSuites)
        {
            try
            {
                return await DoConnect(host, port, version, cipherSuites).ConfigureAwait(false);
            }
            catch (SocketException e)
            {
                _log.LogError($"{e.GetType().Name} occurred {e.Message}{System.Environment.NewLine}{e.StackTrace}");

                return e.SocketErrorCode == SocketError.HostNotFound
                    ? new BouncyCastleTlsTestResult(TlsError.HOST_NOT_FOUND, e.Message, null)
                    : new BouncyCastleTlsTestResult(TlsError.TCP_CONNECTION_FAILED, e.Message, null);
            }
            catch (ArgumentNullException e)
            {
                _log.LogError($"{e.GetType().Name} occurred {e.Message}{System.Environment.NewLine}{e.StackTrace}");
                return new BouncyCastleTlsTestResult(TlsError.TCP_CONNECTION_FAILED, e.Message, null);
            }
            catch (IOException e)
            {
                _log.LogError($"{e.GetType().Name} occurred {e.Message}{System.Environment.NewLine}{e.StackTrace}");
                return new BouncyCastleTlsTestResult(TlsError.TCP_CONNECTION_FAILED, e.Message, null);
            }
            catch (TimeoutException e)
            {
                _log.LogError($"{e.GetType().Name} occurred {e.Message}{System.Environment.NewLine}{e.StackTrace}");
                return new BouncyCastleTlsTestResult(TlsError.TCP_CONNECTION_FAILED, e.Message, null);
            }
            catch (Exception e)
            {
                _log.LogError($"{e.GetType().Name} occurred {e.Message}{System.Environment.NewLine}{e.StackTrace}");
                return new BouncyCastleTlsTestResult(TlsError.INTERNAL_ERROR, e.Message, null);
            }
        }

        private async Task<BouncyCastleTlsTestResult> DoConnect(string host, int port, TlsVersion version, List<CipherSuite> cipherSuites)
        {
            _tcpClient = new TcpClient
            {
                NoDelay = true,
                SendTimeout = _timeOut.Milliseconds,
                ReceiveTimeout = _timeOut.Milliseconds,
            };

            _log.LogDebug($"Starting TCP connection to {host ?? "<null>"}:{port}");
            await _tcpClient.ConnectAsync(host, port).ConfigureAwait(false);
            _log.LogDebug($"Successfully started TCP connection to {host ?? "<null>"}:{port}");

            _log.LogDebug("Initializing session");
            StartTlsResult sessionInitialized = await TryInitializeSession(_tcpClient.GetStream()).ConfigureAwait(false);

            if (!sessionInitialized.Success)
            {
                _log.LogDebug("Failed to initialize session");
                return new BouncyCastleTlsTestResult(TlsError.SESSION_INITIALIZATION_FAILED, sessionInitialized.Error,
                    sessionInitialized.SmtpSession);
            }
            
            _log.LogDebug("Successfully initialized session");

            TestTlsClientProtocol clientProtocol = new TestTlsClientProtocol(_tcpClient.GetStream());

            TestTlsClient testSuiteTlsClient = new TestTlsClient(version, cipherSuites);

            _log.LogDebug("Starting TLS session");
            BouncyCastleTlsTestResult connectionResult = clientProtocol.ConnectWithResults(testSuiteTlsClient);
            _log.LogDebug("Successfully started TLS session");

            return connectionResult;
        }

        public NetworkStream GetStream()
        {
            return _tcpClient?.GetStream();
        }

        //Override this if for example you are using SMTP and you need to STARTTLS
        //before beginning a TLS session.
        public virtual Task<StartTlsResult> TryInitializeSession(NetworkStream stream)
        {
            return Task.FromResult(new StartTlsResult(false, null, string.Empty));
        }

        public void Disconnect()
        {
            _tcpClient?.Dispose();
        }
    }
}
