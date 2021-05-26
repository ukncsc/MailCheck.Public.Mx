using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MailCheck.Mx.BouncyCastle;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Tester;
using MailCheck.Mx.TlsTester.Domain;
using MailCheck.Mx.TlsTester.Factory;
using MailCheck.Mx.TlsTester.Tls;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MailCheck.Mx.TlsTester.MxTester
{
    public interface ITlsSecurityTester
    {
        Task<List<TlsTestResult>> Test(string host);
        Task<List<TlsTestResult>> Test(string host, int[] testIds);
    }

    public class TlsSecurityTester : ITlsSecurityTester
    {
        private const int Port = 25;

        private static JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            Converters =
            {
                new StringEnumConverter()
            }
        };

        private readonly List<ITlsTest> _tests;
        private readonly ITlsClientFactory _tlsClientFactory;
        private readonly ILogger<TlsSecurityTester> _log;

        public TlsSecurityTester(IEnumerable<ITlsTest> tests,
            ITlsClientFactory tlsClientFactory,
            ILogger<TlsSecurityTester> log)
        {
            _tests = tests.OrderBy(_ => _.Id).ToList();
            _tlsClientFactory = tlsClientFactory;
            _log = log;
        }

        public Task<List<TlsTestResult>> Test(string host)
        {
            return Test(host, null);
        }

        public async Task<List<TlsTestResult>> Test(string host, int[] testIds)
        {
            List<TlsTestResult> testResults = new List<TlsTestResult>();
            Stopwatch sw = new Stopwatch();

            List<ITlsTest> testsToRun = (testIds == null || testIds.Length == 0) ? _tests : _tests.Where(t => testIds.Contains(t.Id)).ToList();

            _log.LogDebug($"Beginning test run of {testsToRun.Count} tests for {host ?? "null"}");

            foreach (ITlsTest test in testsToRun)
            {
                sw.Restart();
                _log.LogDebug($"Running test {test.Id} - {test.Name} for {host ?? "null"}");

                ITlsClient tlsClient = _tlsClientFactory.Create();

                try
                {
                    BouncyCastleTlsTestResult result = await tlsClient.Connect(host, Port, test.Version, test.CipherSuites);
                    
                    _log.LogDebug($"Result of test {test.Id} - {test.Name} for {host ?? "null"}:{ Environment.NewLine}{JsonConvert.SerializeObject(result, JsonSerializerSettings)}");

                    testResults.Add(new TlsTestResult(test, result));
                }
                finally
                {
                    _log.LogDebug($"TLS test {test.Id} - {test.Name} for host {host ?? "null"} completed in {sw.ElapsedMilliseconds}ms");
                    tlsClient.Disconnect();
                }
            }

            return testResults;
        }
    }
}