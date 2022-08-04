using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MailCheck.Mx.BouncyCastle.Config;
using MailCheck.Mx.Contracts.SharedDomain;
using Microsoft.Extensions.Logging;
using CipherSuite = MailCheck.Mx.Contracts.SharedDomain.CipherSuite;
using OldApi = MailCheck.Mx.BouncyCastle.OldBouncyCastleTlsApi;
using NewApi = MailCheck.Mx.BouncyCastle.NewBouncyCastleTlsApi;

namespace MailCheck.Mx.BouncyCastle
{
    public interface ITlsClient : IDisposable
    {
        Task<BouncyCastleTlsTestResult> Connect(string host, int port, TlsVersion version, List<CipherSuite> cipherSuites);
    }

    public class TlsClient : ITlsClient
    {
        private readonly TimeSpan _sendReceiveTimeout;
        private readonly TimeSpan _connectionTimeOut;

        private readonly ILogger _log;
        private TcpClient _tcpClient;

        public TlsClient(ILogger<TlsClient> log, IBouncyCastleClientConfig config)
        {
            _log = log;
            _sendReceiveTimeout = config.TcpSendReceiveTimeout;
            _connectionTimeOut = config.TcpConnectionTimeout;
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
            catch (OperationCanceledException e)
            {
                _log.LogError($"{e.GetType().Name} occurred {e.Message}{System.Environment.NewLine}{e.StackTrace}");
                return new BouncyCastleTlsTestResult(TlsError.TCP_CONNECTION_FAILED, e.Message, null);
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
            Stopwatch stopwatch = Stopwatch.StartNew();
            TaskCompletionSource<bool> cancellationCompletionSource = new TaskCompletionSource<bool>();
            using CancellationTokenSource cts = new CancellationTokenSource(_connectionTimeOut);
            using (_tcpClient = new TcpClient
            {
                NoDelay = true,
                SendTimeout = (int)_sendReceiveTimeout.TotalMilliseconds,
                ReceiveTimeout = (int)_sendReceiveTimeout.TotalMilliseconds,
            })
            {
                _log.LogInformation($"Starting TCP connection to {host ?? "<null>"}:{port}");
                Task task = _tcpClient.ConnectAsync(host, port);

                await using (cts.Token.Register(() => cancellationCompletionSource.TrySetResult(true)))
                {
                    if (task != await Task.WhenAny(task, cancellationCompletionSource.Task))
                    {
                        _log.LogInformation($"TCP client timed out after {stopwatch.ElapsedMilliseconds}ms");
                        throw new OperationCanceledException(cts.Token);
                    }
                }

                if (!_tcpClient.Connected)
                {
                    _log.LogInformation($"TCP client failed to connect after {stopwatch.ElapsedMilliseconds}ms");
                    return new BouncyCastleTlsTestResult(TlsError.TCP_CONNECTION_FAILED, string.Empty, null);
                }

                _log.LogInformation($"Successfully started TCP connection to {host ?? "<null>"}:{port}");

                await using NetworkStream stream = _tcpClient.GetStream();

                _log.LogInformation("Initializing session");
                StartTlsResult sessionInitialized = await TryInitializeSession(stream).ConfigureAwait(false);

                if (!sessionInitialized.Success)
                {
                    _log.LogInformation("Failed to initialize session");
                    BouncyCastleTlsTestResult result = new BouncyCastleTlsTestResult(TlsError.SESSION_INITIALIZATION_FAILED, sessionInitialized.Error, sessionInitialized.SmtpSession)
                    {
                        SessionInitialisationResult = sessionInitialized
                    };
                    return result;
                }

                _log.LogInformation("Successfully initialized session");

                ITlsWrapper wrapper;

                if (version == TlsVersion.TlsV13)
                {
                    wrapper = new NewApi.TlsWrapper();
                }
                else
                {
                    wrapper = new OldApi.TlsWrapper();
                }

                _log.LogInformation("Starting TLS session");
                BouncyCastleTlsTestResult connectionResult = wrapper.ConnectWithResults(stream, version, cipherSuites);
                _log.LogInformation("Successfully collected TLS connection result");

                return connectionResult;
            }
        }

        //Override this if for example you are using SMTP and you need to STARTTLS
        //before beginning a TLS session.
        public virtual Task<StartTlsResult> TryInitializeSession(NetworkStream stream)
        {
            return Task.FromResult(new StartTlsResult(false, null, string.Empty));
        }

        public void Dispose()
        {
            _tcpClient?.Dispose();
        }
    }
}
