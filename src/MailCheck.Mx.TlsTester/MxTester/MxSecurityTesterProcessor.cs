﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Mx.Contracts.Tester;
using MailCheck.Mx.TlsTester.Config;
using MailCheck.Mx.TlsTester.Domain;
using MailCheck.Mx.TlsTester.Util;
using Microsoft.Extensions.Logging;

namespace MailCheck.Mx.TlsTester.MxTester
{
    public interface IMxSecurityTesterProcessor
    {
        Task Process(CancellationToken cancellationToken);
    }

    public class MxSecurityTesterProcessor : IMxSecurityTesterProcessor
    {
        private const string TlsHostLogPropertyName = "TlsHost";
        
        private readonly IMxQueueProcessor _mxQueueProcessor;
        private readonly IMessagePublisher _publisher;
        private readonly ITlsSecurityTesterAdapator _mxHostTester;
        private readonly IMxSecurityProcessingFilter _processingFilter;
        private readonly IMxSecurityTesterIgnoredHostsFilter _ignoredHostsFilter;
        private readonly IRecentlyProcessedLedger _recentlyProcessedLedger;
        private readonly IHostClassifier _hostClassifier;
        private readonly IMxTesterConfig _config;
        private readonly ILogger<MxSecurityTesterProcessor> _log;
        private readonly TimeSpan PublishBatchFlushInterval;
        private readonly TimeSpan PrintStatsInterval;
        private readonly Func<ITargetBlock<object>, CancellationToken, Task> RunPipeline;
        
        public MxSecurityTesterProcessor(
            IMxQueueProcessor mxQueueProcessor,
            IMessagePublisher publisher,
            ITlsSecurityTesterAdapator mxHostTester,
            IMxTesterConfig config,
            IMxSecurityProcessingFilter processingFilter,
            IMxSecurityTesterIgnoredHostsFilter ignoredHostsFilter,
            IRecentlyProcessedLedger recentlyProcessedLedger,
            IHostClassifier hostClassifier,
            ILogger<MxSecurityTesterProcessor> log
        ) : this(
            mxQueueProcessor,
            publisher,
            mxHostTester,
            config,
            processingFilter,
            ignoredHostsFilter,
            recentlyProcessedLedger,
            hostClassifier,
            log,
            null)
        { }

        internal MxSecurityTesterProcessor(
            IMxQueueProcessor mxQueueProcessor,
            IMessagePublisher publisher,
            ITlsSecurityTesterAdapator mxHostTester,
            IMxTesterConfig config,
            IMxSecurityProcessingFilter processingFilter,
            IMxSecurityTesterIgnoredHostsFilter ignoredHostsFilter,
            IRecentlyProcessedLedger recentlyProcessedLedger,
            IHostClassifier hostClassifier,
            ILogger<MxSecurityTesterProcessor> log,
            Func<ITargetBlock<object>, CancellationToken, Task> runPipeline)
        {
            _mxQueueProcessor = mxQueueProcessor;
            _publisher = publisher;
            _mxHostTester = mxHostTester;
            _config = config;
            _log = log;
            _recentlyProcessedLedger = recentlyProcessedLedger;
            _processingFilter = processingFilter;
            _ignoredHostsFilter = ignoredHostsFilter;
            _hostClassifier = hostClassifier;
            PublishBatchFlushInterval = TimeSpan.FromSeconds(_config.PublishBatchFlushIntervalSeconds);
            PrintStatsInterval = TimeSpan.FromSeconds(_config.PrintStatsIntervalSeconds);
            
            RunPipeline = runPipeline ?? DefaultRunPipeline;
        }

        public async Task Process(CancellationToken cancellationToken)
        {
            DataflowLinkOptions linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
            DataflowLinkOptions nonPropagatingLinkOptions = new DataflowLinkOptions();
            ExecutionDataflowBlockOptions singleItemBlockOptions = new ExecutionDataflowBlockOptions { BoundedCapacity = 1, EnsureOrdered = false };
            ExecutionDataflowBlockOptions bufferBlockOptions = new ExecutionDataflowBlockOptions { BoundedCapacity = _config.BufferSize, EnsureOrdered = false };
            ExecutionDataflowBlockOptions unboundedBlockOptions = new ExecutionDataflowBlockOptions { EnsureOrdered = false };
            GroupingDataflowBlockOptions batchingBlockOptions = new GroupingDataflowBlockOptions { EnsureOrdered = false };

            TransformManyBlock<object, MxHostTestDetails> queuePoller =
                new TransformManyBlock<object, MxHostTestDetails>(_ => GetMxHostToProcess(_), singleItemBlockOptions);
            
            TransformBlock<MxHostTestDetails, MxHostTestDetails> retestPeriodFilter =
                new TransformBlock<MxHostTestDetails, MxHostTestDetails>((Func<MxHostTestDetails, MxHostTestDetails>)MarkTestToSkip, singleItemBlockOptions);

            BufferBlock<MxHostTestDetails> buffer = new BufferBlock<MxHostTestDetails>(bufferBlockOptions);

            TransformManyBlock<MxHostTestDetails, MxHostTestDetails> duplicateFilter =
                new TransformManyBlock<MxHostTestDetails, MxHostTestDetails>(_ => FilterHosts(_), singleItemBlockOptions);

            List<TransformBlock<MxHostTestDetails, MxHostTestDetails>> classifiers = Enumerable
                .Range(1, _config.TlsTesterThreadCount)
                .Select(index => new TransformBlock<MxHostTestDetails, MxHostTestDetails>(CreateTlsClassifier(Guid.NewGuid()), singleItemBlockOptions))
                .ToList();

            OverflowingBufferBlock<MxHostTestDetails> slowLaneBuffer = new OverflowingBufferBlock<MxHostTestDetails>(_config.BufferSize);
            
            BufferBlock<MxHostTestDetails> fastLaneBuffer = new BufferBlock<MxHostTestDetails>(bufferBlockOptions);

            List<TransformBlock<MxHostTestDetails, MxHostTestDetails>> fastLaneProcessors = Enumerable
                .Range(1, _config.TlsTesterThreadCount)
                .Select(index => new TransformBlock<MxHostTestDetails, MxHostTestDetails>(CreateTlsTester(Guid.NewGuid()), singleItemBlockOptions))
                .ToList();

            List<TransformBlock<MxHostTestDetails, MxHostTestDetails>> slowLaneProcessors = Enumerable
                .Range(1, _config.TlsTesterThreadCount)
                .Select(index => new TransformBlock<MxHostTestDetails, MxHostTestDetails>(CreateTlsTester(Guid.NewGuid()), singleItemBlockOptions))
                .ToList();

            BatchBlock<MxHostTestDetails> resultBatcher =
                new BatchBlock<MxHostTestDetails>(_config.PublishBatchSize, batchingBlockOptions);

            Timer timer = new Timer(_ =>
            {
                resultBatcher.TriggerBatch();
                _log.LogDebug("Batch triggered.");
            });

            TransformBlock<MxHostTestDetails, MxHostTestDetails> batchFlusher =
                new TransformBlock<MxHostTestDetails, MxHostTestDetails>(ResetTimer(timer), unboundedBlockOptions);

            TransformBlock<MxHostTestDetails[], MxHostTestDetails[]> resultPublisher =
                new TransformBlock<MxHostTestDetails[], MxHostTestDetails[]>(_ => PublishResults(_), unboundedBlockOptions);

            ActionBlock<MxHostTestDetails[]> deleteFromQueue =
                new ActionBlock<MxHostTestDetails[]>(_ => RemoveFromQueue(_), unboundedBlockOptions);

            queuePoller.LinkTo(retestPeriodFilter, linkOptions);
            retestPeriodFilter.LinkTo(batchFlusher, nonPropagatingLinkOptions, x => x.SkipTesting);
            retestPeriodFilter.LinkTo(buffer, linkOptions, x => !x.SkipTesting);

            buffer.LinkTo(duplicateFilter, linkOptions);

            classifiers.ForEach(processor => {
                duplicateFilter.LinkTo(processor, linkOptions);
                processor.LinkTo(slowLaneBuffer.Target, nonPropagatingLinkOptions, item => item.Classification == Classifications.Slow);
                processor.LinkTo(fastLaneBuffer, nonPropagatingLinkOptions, item => item.Classification == Classifications.Fast);
                processor.LinkTo(batchFlusher, nonPropagatingLinkOptions, item => item.Classification == Classifications.Unknown);
            });

            slowLaneProcessors.ForEach(processor => {
                slowLaneBuffer.Source.LinkTo(processor, linkOptions);
                processor.LinkTo(batchFlusher, nonPropagatingLinkOptions);
            });

            fastLaneProcessors.ForEach(processor => {
                fastLaneBuffer.LinkTo(processor, linkOptions);
                processor.LinkTo(batchFlusher, nonPropagatingLinkOptions);
            });

            slowLaneBuffer.Overflow.LinkTo(batchFlusher, nonPropagatingLinkOptions);

            batchFlusher.LinkTo(resultBatcher, linkOptions);
            resultBatcher.LinkTo(resultPublisher, linkOptions);
            resultPublisher.LinkTo(deleteFromQueue, linkOptions);

            Dictionary<string, Func<int>> diagnosticStatsMetrics = new Dictionary<string, Func<int>> {
                ["Queued"] = () => queuePoller.OutputCount + buffer.Count,
                ["Processing"] = () => _processingFilter.HostCount,
            };

            Task[] classifierTasks = classifiers.Select(processor => processor.Completion).ToArray();
            Task[] fastLaneTasks = fastLaneProcessors.Select(processor => processor.Completion).ToArray();

            // Start the stats print loop
            Task statsTask = PrintStats(diagnosticStatsMetrics, cancellationToken);

            await RunPipeline(queuePoller, cancellationToken);

            _log.LogInformation("Shutting down TLS Tester");

            queuePoller.Complete();

            _log.LogInformation("Waiting for classifiers to complete...");
            await Task.WhenAll(classifierTasks);

            slowLaneBuffer.Target.Complete();
            fastLaneBuffer.Complete();

            _log.LogInformation("Waiting for fast lane processors to complete...");
            await Task.WhenAll(fastLaneTasks);

            _log.LogInformation("Test processors complete. Flushing results...");

            batchFlusher.Complete();

            _log.LogInformation("Waiting for results flush and final shutdown...");

            await Task.WhenAll(
                statsTask,
                deleteFromQueue.Completion
            );

            _log.LogInformation("TLS tester shut down. Exiting.");
        }

        private async Task RemoveFromQueue(MxHostTestDetails[] messages)
        {
            foreach (MxHostTestDetails tlsTestResult in messages)
            {
                TlsTestPending test = tlsTestResult.Test;
                string hostname = test.Id;
                string normalizedHostname = tlsTestResult.NormalizedHostname;
                string messageId = test.MessageId;
                string receiptHandle = test.ReceiptHandle;

                using (_log.BeginScope(new Dictionary<string, object> { [TlsHostLogPropertyName] = hostname }))
                {
                    try
                    {
                        if (tlsTestResult.PublishedResultsSuccessfully)
                        {
                            _recentlyProcessedLedger.Set(normalizedHostname);
                        }

                        if (tlsTestResult.PublishedResultsSuccessfully || tlsTestResult.SkipTesting)
                        {
                            _log.LogInformation($"Deleting message from sqs for host: {hostname} - Message Id: {messageId}");
                            await _mxQueueProcessor.DeleteMessage(messageId, receiptHandle);
                            _log.LogInformation($"Deleted message from sqs for host: {hostname} - Message Id: {messageId}");
                        }
                        else
                        {
                            _log.LogInformation($"Returning message to queue - failed to retrieve or publish results for host {hostname}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.LogError(ex, $"Error occurred deleting message {test.MessageId} from queue for hostname {hostname}");
                    }
                    finally
                    {
                        _processingFilter.ReleaseReservation(hostname);
                    }
                }
            }
        }

        private async Task<IEnumerable<MxHostTestDetails>> GetMxHostToProcess(object _)
        {
            List<MxHostTestDetails> hosts = new List<MxHostTestDetails>();

            try
            {
                _log.LogInformation("Getting mx hosts to process.");

                List<TlsTestPending> testsPending = await _mxQueueProcessor.GetMxHosts();

                hosts.AddRange(testsPending.Select(tp => new MxHostTestDetails(tp) { NormalizedHostname = tp.Id.Trim().TrimEnd('.').ToLowerInvariant() }));

                _log.LogInformation(testsPending.Count > 0
                    ? $"Found {testsPending.Count} mx hosts to test: {Environment.NewLine}{string.Join(Environment.NewLine, testsPending.Select(pendingTest => pendingTest.Id))}"
                    : "Didn't find any hosts to test.");
            }
            catch (Exception e)
            {
                _log.LogError(e, $"Error occured fetching mx hosts to test");
            }

            return hosts;
        }

        private MxHostTestDetails MarkTestToSkip(MxHostTestDetails host)
        {
            using (_log.BeginScope(new Dictionary<string, object> { [TlsHostLogPropertyName] = host.Test.Id }))
            {
                if (_ignoredHostsFilter.IsIgnored(host.NormalizedHostname))
                {
                    host.SkipTesting = true;

                    _log.LogInformation($"Exclusion status for {host}: Host will be ignored in Tls Testing");

                    return host;
                }

                host.SkipTesting = _recentlyProcessedLedger.Contains(host.NormalizedHostname);

                _log.LogInformation($"Host {host.Test.Id} will be {(host.SkipTesting ? "skipped" : "processed")}");
                return host;
            }
        }


        private IEnumerable<MxHostTestDetails> FilterHosts(MxHostTestDetails host)
        {
            string hostname = host.Test.Id;
            using (_log.BeginScope(new Dictionary<string, object> { [TlsHostLogPropertyName] = hostname }))
            {
                if (_processingFilter.Reserve(hostname))
                {
                    yield return host;
                }
            }
        }

        private Func<MxHostTestDetails, Task<MxHostTestDetails>> CreateTlsClassifier(Guid testerId)
        {
            return async testDetails =>
            {
                TlsTestPending tlsTest = testDetails.Test;

                using (_log.BeginScope(new Dictionary<string, object> { ["TlsTesterId"] = testerId, [TlsHostLogPropertyName] = tlsTest.Id }))
                {
                    try
                    {
                        ClassificationResult classification = await _hostClassifier.Classify(tlsTest);

                        testDetails.Classification = classification.Classification;
                    }
                    catch (Exception e)
                    {
                        _log.LogError(e, $"Error occurred during canary test run");
                    }

                    return testDetails;
                }
            };
        }

        private Func<MxHostTestDetails, Task<MxHostTestDetails>> CreateTlsTester(Guid testerId)
        {
            return async testDetails =>
            {
                TlsTestPending tlsTest = testDetails.Test;

                using (_log.BeginScope(new Dictionary<string, object> { ["TlsTesterId"] = testerId, [TlsHostLogPropertyName] = tlsTest.Id }))
                {
                    try
                    {
                        Stopwatch stopwatch = Stopwatch.StartNew();
                        _log.LogInformation($"Starting TLS test run");

                        //TODO remove after simplified tester switchover
                        var temporaryNonTestedResult = new TlsTestResults(tlsTest.Id, false, false, null, null, null,
                            null, null, null, null, null, null, null, null, null, null, null);
                        testDetails.TestResults = temporaryNonTestedResult;
                        _log.LogInformation($"Completed TLS test run, time taken : {stopwatch.ElapsedMilliseconds}ms");
                    }
                    catch (Exception e)
                    {
                        _log.LogError(e, $"Error occurred during TLS test run");
                    }

                    return testDetails;
                }
            };
        }

        private async Task<MxHostTestDetails[]> PublishResults(MxHostTestDetails[] tlsTestResults)
        {
            foreach (MxHostTestDetails tlsTestResult in tlsTestResults)
            {
                string hostname = tlsTestResult.Test.Id;

                using (_log.BeginScope(new Dictionary<string, object> { [TlsHostLogPropertyName] = hostname }))
                {
                    if (tlsTestResult.TestResults == null)
                    {
                        _log.LogInformation($"Skipping publish - no result for smtp test for host: {hostname}");
                        continue;
                    }

                    try
                    {
                        _log.LogInformation($"Publishing smtp test results for host: {hostname}");
                        await _publisher.Publish(tlsTestResult.TestResults, _config.SnsTopicArn);
                        tlsTestResult.PublishedResultsSuccessfully = true;
                        _log.LogInformation($"Published smtp test results for host: {hostname}");
                    }
                    catch (Exception ex)
                    {
                        _log.LogError(ex, $"Error occurred publishing results for host: {hostname})");
                    }
                }
            }

            return tlsTestResults;
        }

        private Func<MxHostTestDetails, MxHostTestDetails> ResetTimer(Timer timer)
        {
            return (tlsTestResults) =>
            {
                try
                {
                    timer.Change(PublishBatchFlushInterval, Timeout.InfiniteTimeSpan);
                }
                catch (Exception e)
                {
                    _log.LogError(e, $"The following error occured resetting batch timeout timer");
                }

                return tlsTestResults;
            };
        }

        private async Task PrintStats(Dictionary<string, Func<int>> metricsFuncs, CancellationToken cancellationToken)
        {
            if (PrintStatsInterval == TimeSpan.Zero) return;

            Task cancelled = cancellationToken.WhenCanceled();

            while (!cancellationToken.IsCancellationRequested)
            {
                _log.LogInformation($"TLS Tester stats: {string.Join(", ", metricsFuncs.Select(x => $"{x.Key}: {x.Value()}"))}");

                Task delay = Task.Delay(PrintStatsInterval);
                await Task.WhenAny(delay, cancelled);
            }
        }

        private static async Task DefaultRunPipeline(ITargetBlock<object> inputQueue, CancellationToken cancellationToken)
        {
            Task cancelled = cancellationToken.WhenCanceled();

            while (!cancellationToken.IsCancellationRequested)
            {
                // This SendAsync task will not complete until the queue poller is ready to 
                // accept a new element which will be after a previous poll has completed (20s long-poll)
                // and the buffers from the previous poll have emptied i.e. the dequeued items 
                // have been accepted by the next block.
                Task queuePoll = inputQueue.SendAsync(null);

                await Task.WhenAny(queuePoll, cancelled);
            }
        }
    }
}
