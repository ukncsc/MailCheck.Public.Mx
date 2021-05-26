using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MailCheck.Mx.Contracts.Tester;
using MailCheck.Mx.TlsTester.Config;
using MailCheck.Mx.TlsTester.Domain;

namespace MailCheck.Mx.TlsTester.MxTester
{
    public enum Classifications
    {
        Unknown = 0,
        Fast = 1,
        Slow = 2,
    }

    public class ClassificationResult
    {
        public Classifications Classification { get; set; }
    }

    public interface IHostClassifier
    {
        Task<ClassificationResult> Classify(TlsTestPending tlsTest);
    }

    public class HostClassifier : IHostClassifier
    {
        private static readonly int[] FirstTestOnly = new int[] { 1 };
        internal static readonly TlsTestResults TimeoutResult = TlsTestResults.CreateNullResults();

        private readonly ITlsSecurityTesterAdapator _hostTester;
        private readonly ILogger<HostClassifier> _log;
        private readonly TimeSpan SlowResponseThreshold;

        private readonly Func<TimeSpan, TlsTestResults, CancellationToken, Task<TlsTestResults>> DelayFunc;

        public HostClassifier(
            ITlsSecurityTesterAdapator mxHostTester,
            IMxTesterConfig config,
            ILogger<HostClassifier> log) : this(mxHostTester, config, log, null)
        {
        }

        internal HostClassifier(
            ITlsSecurityTesterAdapator mxHostTester,
            IMxTesterConfig config,
            ILogger<HostClassifier> log,
            Func<TimeSpan, TlsTestResults, CancellationToken, Task<TlsTestResults>> delayFunc)
        {
            _hostTester = mxHostTester;
            _log = log;

            SlowResponseThreshold = TimeSpan.FromSeconds(config.SlowResponseThresholdSeconds);
            DelayFunc = delayFunc ?? Delayed;
        }

        public async Task<ClassificationResult> Classify(TlsTestPending tlsTest)
        {
            _log.LogDebug($"Starting canary test run for host {tlsTest.Id}.");

            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                TlsTestResults result = await await Task.WhenAny(
                    _hostTester.Test(tlsTest, FirstTestOnly),
                    DelayFunc(SlowResponseThreshold, TimeoutResult, tokenSource.Token));

                Classifications classification;
                if (ReferenceEquals(result, TimeoutResult))
                {
                    _log.LogDebug($"Canary test timed out for host {tlsTest.Id}.");
                    classification = Classifications.Slow;
                }
                else
                {
                    classification = Classifications.Fast;
                    tokenSource.Cancel();
                    _log.LogDebug($"Completed canary test run without timeout for host {tlsTest.Id}.");
                }

                return new ClassificationResult
                {
                    Classification = classification
                };
            }
        }

        private static async Task<TlsTestResults> Delayed(TimeSpan delay, TlsTestResults result, CancellationToken cancellationToken)
        {
            await Task.Delay(delay, cancellationToken);
            return result;
        }
    }
}
