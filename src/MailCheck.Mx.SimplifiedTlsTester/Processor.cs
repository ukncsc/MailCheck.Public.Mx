using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Mx.Contracts.Simplified;
using MailCheck.Mx.SimplifiedTlsTester.TestRunner;
using Microsoft.Extensions.Logging;

namespace MailCheck.Mx.SimplifiedTlsTester
{
    public interface IProcessor
    {
        Task Process(CancellationToken cancellationToken);
    }

    public class Processor : IProcessor
    {
        private readonly ISqsClient _sqsClient;
        private readonly ITestRunner _testRunner;
        private readonly IMessagePublisher _publisher;
        private readonly string _snsTopicArn;
        private readonly ILogger<Processor> _log;
        private readonly TimeSpan _testRunTimeout;
        internal Task TimeoutTaskOverride;

        public Processor(
            ISqsClient sqsClient,
            ITestRunner testRunner,
            IMessagePublisher publisher,
            IProcessorConfig processorConfig,
            ILogger<Processor> log)
        {
            _sqsClient = sqsClient;
            _testRunner = testRunner;
            _publisher = publisher;
            _snsTopicArn = processorConfig.SnsTopicArn;
            _testRunTimeout = processorConfig.TestRunTimeout;
            _log = log;
        }

        public async Task Process(CancellationToken cancellationToken)
        {
            try
            {
                _log.LogInformation("Starting process loop...");
                while (!cancellationToken.IsCancellationRequested)
                {
                    List<SimplifiedTlsTestPending> messages = await _sqsClient.GetTestsPending(cancellationToken);
                    if (messages.Count == 0) continue;

                    var testRunId = Guid.NewGuid();
                    var ipsTested = string.Join(",", messages.Select(t => t.Id));

                    using (_log.BeginScope(new Dictionary<string, string>
                    {
                        ["TestRunId"] = testRunId.ToString(),
                        ["TestRunIps"] = ipsTested
                    }))
                    {
                        _log.LogInformation($"Starting test run {testRunId.ToString()} for {messages.Count} pending tests.");
                        var sw = Stopwatch.StartNew();

                        List<Task<SimplifiedTlsTestResults>> testRunnerTasks = messages.Select(async x =>
                        {
                            string ipAddress = x.Id;

                            using (_log.BeginScope(new Dictionary<string, string> { ["IpAddress"] = ipAddress }))
                            {
                                try
                                {
                                    _log.LogInformation($"Starting test run for IP {ipAddress}");

                                    SimplifiedTlsTestResults testResult = await _testRunner.Run(ipAddress);
                                    if (testResult.Inconclusive)
                                    {
                                        _log.LogInformation($"Test run was inconclusive for IP {ipAddress}");
                                        return null;
                                    }

                                    _log.LogInformation($"Finishing test run for IP {ipAddress}");

                                    testResult.CausationId = x.MessageId;
                                    testResult.CorrelationId = x.CorrelationId;

                                    return testResult;
                                }
                                catch (Exception e)
                                {
                                    _log.LogError(e, $"Runner failed with exception testing IP {ipAddress}");
                                    return null;
                                }
                            }
                        }).ToList();

                        var timeoutTask = TimeoutTaskOverride ?? Task.Delay(_testRunTimeout, cancellationToken);
                        Task taskResult = await Task.WhenAny(Task.WhenAll(testRunnerTasks), timeoutTask);

                        var testRunLog = new StringBuilder();
                        
                        testRunLog.AppendLine("Test run log");

                        if (taskResult == timeoutTask)
                        {
                            testRunLog.AppendLine($"Test run {testRunId.ToString()} timed out after {sw.ElapsedMilliseconds}ms");
                        }
                        else
                        {
                            testRunLog.AppendLine($"Test run {testRunId.ToString()} completed successfully after {sw.ElapsedMilliseconds}ms");
                        }

                        string statuses = string.Join(",", testRunnerTasks
                            .GroupBy(x => x.Status)
                            .Select(x => $"Status {x.Key}: {x.Count()}"));

                        testRunLog.AppendLine($"Test run tasks status: {statuses}");

                        SimplifiedTlsTestResults[] testResults = testRunnerTasks
                            .Where(x => x.IsCompletedSuccessfully && x.Result != null)
                            .Select(t => t.Result)
                            .ToArray();

                        testRunLog.AppendLine($"Total run tasks complete with result: {testResults.Length}");

                        List<string> failed = messages.Select(x => x.Id).Except(testResults.Select(x => x.Id)).ToList();
                        if (failed.Count > 0)
                        {
                            testRunLog.AppendLine($"Failed to process ipAddresses: {string.Join(",", failed)}");
                        }

                        _log.LogInformation(testRunLog.ToString());

                        foreach (SimplifiedTlsTestResults simplifiedTlsTestResult in testResults.Where(x => x != null))
                        {
                            await _publisher.Publish(simplifiedTlsTestResult, _snsTopicArn);
                            _log.LogInformation($"Published SimplifiedTlsTestResults for ip: {simplifiedTlsTestResult.Id}");
                        }

                        _log.LogInformation($"Finished publishing results after {sw.ElapsedMilliseconds}ms");

                        _log.LogInformation($"{testResults.Length} of {messages.Count} messages processed successfully");

                        _log.LogInformation($"Deleting {messages.Count} messages");
                        await _sqsClient.DeleteMessages(messages);

                        _log.LogInformation($"End of test run after {sw.ElapsedMilliseconds}ms");
                    }
                }
                _log.LogInformation("Ending process loop.");
            }
            catch (OperationCanceledException oc)
            {
                _log.LogInformation(oc, "Caught operation cancelled exception");
            }
        }
    }
}
