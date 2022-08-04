using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Simplified;
using MailCheck.Mx.SimplifiedTlsTester.Domain;
using MailCheck.Mx.SimplifiedTlsTester.Rules;
using Microsoft.Extensions.Logging;

namespace MailCheck.Mx.SimplifiedTlsTester.TestRunner
{
    public interface ITestRunner
    {
        Task<SimplifiedTlsTestResults> Run(string ipAddress);
    }

    public class TestRunner : ITestRunner
    {
        private static readonly LinkedList<ITlsRule> DefaultRules = new LinkedList<ITlsRule>(new ITlsRule[]
        {
            new Tls13Rule(),
            new Tls12GoodCiphersRule(),
            new Tls12ServerPreferenceRule(),
            null
        });

        private readonly LinkedList<ITlsRule> _rules;
        private readonly ITlsTester _tester;
        private readonly ILogger<TestRunner> _logger;

        public TestRunner(ITlsTester tester, ILogger<TestRunner> logger) : this(tester, logger, null)
        {
        }

        internal TestRunner(ITlsTester tester, ILogger<TestRunner> logger, LinkedList<ITlsRule> rules)
        {
            _tester = tester;
            _logger = logger;
            _rules = rules ?? DefaultRules;
        }

        public async Task<SimplifiedTlsTestResults> Run(string ipAddress)
        {
            TestContext context = new TestContext();

            LinkedListNode<ITlsRule> current = _rules.First;

            List<SimplifiedTlsConnectionResult> simplifiedTlsConnectionResults = new List<SimplifiedTlsConnectionResult>();
            ConcurrentDictionary<string, string> certificates = new ConcurrentDictionary<string, string>();

            while (current?.Value != null)
            {
                context.CurrentTest = current;
                context.NextTest = current.Next;
                ITlsRule tlsRule = current.Value;

                using (_logger.BeginScope(new Dictionary<string, string> { ["TestName"] = tlsRule.TestCriteria.Name }))
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    _logger.LogInformation($"Running test {tlsRule.TestCriteria.Name} for {ipAddress} on thread {Thread.CurrentThread.ManagedThreadId}");

                    BouncyCastleTlsTestResult result = await _tester.RunTest(tlsRule.TestCriteria, ipAddress);

                    foreach (X509Certificate2 certificate in result.Certificates)
                    {
                        certificates.GetOrAdd(certificate.Thumbprint, _ => Convert.ToBase64String(certificate.RawData));
                    }

                    SimplifiedTlsConnectionResult connectionResult = new SimplifiedTlsConnectionResult
                    {
                        TestName = tlsRule.TestCriteria.Name,
                        CipherSuite = result.CipherSuite.ToString(),
                        CertificateThumbprints = result.Certificates.Select(_ => _.Thumbprint).ToArray(),
                        Error = result.TlsError.HasValue ? result.TlsError.ToString() : null,
                        ErrorDescription = result.ErrorDescription,
                        SmtpHandshake = result.SmtpResponses == null ? "" : string.Join(Environment.NewLine, result.SmtpResponses)
                    };

                    if (connectionResult.Error == null)
                    {
                        _logger.LogInformation($"Test {tlsRule.TestCriteria.Name} for {ipAddress} complete after {sw.ElapsedMilliseconds}ms. Result: No error Cipher: {connectionResult.CipherSuite} Certificates: {connectionResult.CertificateThumbprints.Length}");
                    }
                    else
                    {
                        _logger.LogInformation($"Test {tlsRule.TestCriteria.Name} for {ipAddress} complete after {sw.ElapsedMilliseconds}ms. Result: {connectionResult.Error} Error: {connectionResult.ErrorDescription}");
                    }

                    simplifiedTlsConnectionResults.Add(connectionResult);

                    current = tlsRule.Evaluate(context, result);
                }
            }

            if (context.Inconclusive)
            {
                context.Advisories = null;
            }

            return new SimplifiedTlsTestResults(ipAddress)
            {
                AdvisoryMessages = context.Advisories?.ToList(),
                SimplifiedTlsConnectionResults = simplifiedTlsConnectionResults,
                Certificates = new Dictionary<string, string>(certificates),
                Inconclusive = context.Inconclusive
            };
        }
    }
}
