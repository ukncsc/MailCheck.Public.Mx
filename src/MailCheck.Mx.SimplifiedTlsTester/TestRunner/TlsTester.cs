using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MailCheck.Mx.BouncyCastle;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.SimplifiedTlsTester.Domain;
using Microsoft.Extensions.Logging;

namespace MailCheck.Mx.SimplifiedTlsTester.TestRunner
{
    public interface ITlsTester
    {
        Task<BouncyCastleTlsTestResult> RunTest(TestCriteria stageTestCriteriaCriteria, string ipAddress);
    }

    public class TlsTester : ITlsTester
    {
        private const int Port = 25;
        private readonly Func<ITlsClient> _tlsClientFactory;
        private readonly ILogger<TlsTester> _log;
        private readonly IContainerConfig _containerConfig;
        private readonly Guid _name;

        public TlsTester(Func<ITlsClient> tlsClientFactory, ILogger<TlsTester> log, IContainerConfig containerConfig)
        {
            _name = Guid.NewGuid();
            _tlsClientFactory = tlsClientFactory;
            _log = log;
            _containerConfig = containerConfig;
            
            _log.LogInformation($"TlsTester {_name} says hello");
        }

        public async Task<BouncyCastleTlsTestResult> RunTest(TestCriteria stageTestCriteriaCriteria, string ipAddress)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            _log.LogInformation($"Test for IP address {ipAddress} starting on TlsTester {_name} on thread {Thread.CurrentThread.ManagedThreadId}");
            using ITlsClient tlsClient = _tlsClientFactory();

            BouncyCastleTlsTestResult result = await tlsClient.Connect(ipAddress, Port, stageTestCriteriaCriteria.Protocol, stageTestCriteriaCriteria.CipherSuites.ToList());

            if (result.TlsError != null)
            {
                _containerConfig.LogContainerDetails(_log);
            }

            _log.LogInformation($"Test for IP address {ipAddress} finished after {stopwatch.ElapsedMilliseconds}ms on TlsTester {_name} on thread {Thread.CurrentThread.ManagedThreadId}");
            return result;
        }
    }
}