using System;
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
        private readonly ITlsSecurityTesterAdapator  _mxHostTester;
        private readonly IMxSecurityProcessingFilter _processingFilter;
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
            ILogger<MxSecurityTesterProcessor> log
        ) : this(
            mxQueueProcessor,
            publisher,
            mxHostTester,
            config,
            processingFilter,
            log,
            null
        ) { }

        internal MxSecurityTesterProcessor(
            IMxQueueProcessor mxQueueProcessor,
            IMessagePublisher publisher,
            ITlsSecurityTesterAdapator mxHostTester,
            IMxTesterConfig config,
            IMxSecurityProcessingFilter processingFilter,
            ILogger<MxSecurityTesterProcessor> log,
            Func<ITargetBlock<object>, CancellationToken, Task> runPipeline)
        {
            _mxQueueProcessor = mxQueueProcessor;
            _publisher = publisher;
            _mxHostTester = mxHostTester;
            _config = config;
            _log = log;
            _processingFilter = processingFilter;
            PublishBatchFlushInterval = TimeSpan.FromSeconds(_config.PublishBatchFlushIntervalSeconds);
            PrintStatsInterval = TimeSpan.FromSeconds(_config.PrintStatsIntervalSeconds);
            RunPipeline = runPipeline ?? DefaultRunPipeline;
        }

        public async Task Process(CancellationToken cancellationToken)
        {
            DataflowLinkOptions linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
            ExecutionDataflowBlockOptions singleItemBlockOptions = new ExecutionDataflowBlockOptions { BoundedCapacity = 1, EnsureOrdered = false };
            ExecutionDataflowBlockOptions bufferBlockOptions = new ExecutionDataflowBlockOptions { BoundedCapacity = 100, EnsureOrdered = false };
            ExecutionDataflowBlockOptions unboundedBlockOptions = new ExecutionDataflowBlockOptions { EnsureOrdered = false };
            GroupingDataflowBlockOptions batchingBlockOptions = new GroupingDataflowBlockOptions { EnsureOrdered = false };

            TransformManyBlock<object, TlsTestPending> queuePoller =
                new TransformManyBlock<object, TlsTestPending>(_ => GetMxHostToProcess(_), singleItemBlockOptions);

            BufferBlock<TlsTestPending> buffer = new BufferBlock<TlsTestPending>(bufferBlockOptions);

            TransformManyBlock<TlsTestPending, TlsTestPending> duplicateFilter =
                new TransformManyBlock<TlsTestPending, TlsTestPending>(_ => FilterHosts(_), singleItemBlockOptions);

            List<TransformBlock<TlsTestPending, MxHostTestDetails>> mxTestProcessors = Enumerable
                .Range(1, _config.TlsTesterThreadCount)
                .Select(index => new TransformBlock<TlsTestPending, MxHostTestDetails>(CreateTlsTester(Guid.NewGuid()), singleItemBlockOptions))
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

            queuePoller.LinkTo(buffer, linkOptions);
            buffer.LinkTo(duplicateFilter, linkOptions);

            mxTestProcessors.ForEach(processor => {
                duplicateFilter.LinkTo(processor, linkOptions);
                processor.LinkTo(batchFlusher); // No completion propogate here
            });

            batchFlusher.LinkTo(resultBatcher, linkOptions);
            resultBatcher.LinkTo(resultPublisher, linkOptions);
            resultPublisher.LinkTo(deleteFromQueue, linkOptions);

            var blocks = new Dictionary<string, Func<int>> {
                ["Queued"] = () => queuePoller.OutputCount + buffer.Count,
                ["Processing"] = () => _processingFilter.HostCount,
            };

            var processorTasks = mxTestProcessors.Select(processor => processor.Completion).ToArray();

            // Start the stats print loop
            var statsTask = PrintStats(blocks, cancellationToken);

            await RunPipeline(queuePoller, cancellationToken);

            _log.LogInformation("Shutting down TLS Tester");

            queuePoller.Complete();

            _log.LogInformation("Waiting for test processors to complete...");

            await Task.WhenAll(processorTasks);

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
                var test = tlsTestResult.Test;
                var hostname = test.Id;
                var messageId = test.MessageId;
                var receiptHandle = test.ReceiptHandle;

                using (_log.BeginScope(new Dictionary<string, object> { [TlsHostLogPropertyName] = hostname }))
                {
                    try
                    {
                        if (tlsTestResult.PublishedResultsSuccessfully)
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

        private async Task<IEnumerable<TlsTestPending>> GetMxHostToProcess(object _)
        {
            List<TlsTestPending> hosts = new List<TlsTestPending>();

            try
            {
                _log.LogDebug("Getting mx hosts to process.");

                hosts.AddRange(await _mxQueueProcessor.GetMxHosts());

                _log.LogDebug(hosts.Count > 0
                    ? $"Found {hosts.Count} mx hosts to test: {Environment.NewLine}{string.Join(Environment.NewLine, hosts.Select(pendingTest => pendingTest.Id))}"
                    : "Didn't find any hosts to test.");
            }
            catch (Exception e)
            {
                _log.LogError(e, $"Error occured fetching mx hosts to test");
            }

            return hosts.ToArray();
        }

        private IEnumerable<TlsTestPending> FilterHosts(TlsTestPending host)
        {
            using (_log.BeginScope(new Dictionary<string, object> { [TlsHostLogPropertyName] = host.Id }))
            {
                if (_processingFilter.Reserve(host.Id))
                {
                    yield return host;
                }
            }
        }

        private Func<TlsTestPending, Task<MxHostTestDetails>> CreateTlsTester(Guid testerId)
        {
            return async tlsTest =>
            {
                using (_log.BeginScope(new Dictionary<string, object> { ["TlsTesterId"] = testerId, [TlsHostLogPropertyName] = tlsTest.Id }))
                {
                    try
                    {
                        var stopwatch = Stopwatch.StartNew();
                        _log.LogInformation($"Starting TLS test run");
                        TlsTestResults tlsTestResults = await _mxHostTester.Test(tlsTest);
                        _log.LogInformation($"Completed TLS test run, time taken : {stopwatch.ElapsedMilliseconds}ms");
                        return new MxHostTestDetails(tlsTestResults, tlsTest);
                    }
                    catch (Exception e)
                    {
                        _log.LogError(e, $"Error occurred during TLS test run");
                    }

                    return new MxHostTestDetails(null, tlsTest);
                }
            };
        }

        private async Task<MxHostTestDetails[]> PublishResults(MxHostTestDetails[] tlsTestResults)
        {
            foreach (MxHostTestDetails tlsTestResult in tlsTestResults)
            {
                var hostname = tlsTestResult.Test.Id;

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

        private async Task PrintStats(Dictionary<string, Func<int>> blocks, CancellationToken cancellationToken)
        {
            if (PrintStatsInterval == TimeSpan.Zero) return;

            Task cancelled = cancellationToken.WhenCanceled();

            while (!cancellationToken.IsCancellationRequested)
            {
                _log.LogInformation($"TLS Tester stats: {string.Join(", ", blocks.Select(x => $"{x.Key}: {x.Value()}"))}");

                Task delay = Task.Delay(PrintStatsInterval);
                await Task.WhenAny(delay, cancelled);
            }
        }

        private async Task DefaultRunPipeline(ITargetBlock<object> inputQueue, CancellationToken cancellationToken)
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
