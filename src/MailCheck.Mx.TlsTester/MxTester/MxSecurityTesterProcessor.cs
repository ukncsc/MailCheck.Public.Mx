using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly IMxQueueProcessor _mxQueueProcessor;
        private readonly IMessagePublisher _publisher;
        private readonly ITlsSecurityTesterAdapator  _mxHostTester;
        private readonly IMxSecurityProcessingFilter _processingFilter;
        private readonly IMxTesterConfig _config;
        private readonly ILogger<MxSecurityTesterProcessor> _log;

        public MxSecurityTesterProcessor(
            IMxQueueProcessor mxQueueProcessor,
            IMessagePublisher publisher,
            ITlsSecurityTesterAdapator mxHostTester,
            IMxTesterConfig config,
            IMxSecurityProcessingFilter processingFilter,
            ILogger<MxSecurityTesterProcessor> log)
        {
            _mxQueueProcessor = mxQueueProcessor;
            _publisher = publisher;
            _mxHostTester = mxHostTester;
            _config = config;
            _log = log;
            _processingFilter = processingFilter;
        }

        public async Task Process(CancellationToken cancellationToken)
        {
            DataflowLinkOptions linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
            ExecutionDataflowBlockOptions pipelineStartBlockOptions = new ExecutionDataflowBlockOptions { BoundedCapacity = 10 };
            ExecutionDataflowBlockOptions preBatchBlockOptions = new ExecutionDataflowBlockOptions { BoundedCapacity = 500 };
            ExecutionDataflowBlockOptions parallelPreBatchBlockOptions = new ExecutionDataflowBlockOptions { BoundedCapacity = 500, MaxDegreeOfParallelism = 20, EnsureOrdered = false };
            ExecutionDataflowBlockOptions postBatchBlockOptions = new ExecutionDataflowBlockOptions { BoundedCapacity = 50 };
            GroupingDataflowBlockOptions postBatchGroupingBlockOptions = new GroupingDataflowBlockOptions { BoundedCapacity = 50 };

            BufferBlock<object> pollQueue = new BufferBlock<object>(pipelineStartBlockOptions);

            TransformManyBlock<object, TlsTestPending> mxHostToProcessQueue =
                new TransformManyBlock<object, TlsTestPending>(async _ => await GetMxHostToProcess(), preBatchBlockOptions);

            TransformManyBlock<TlsTestPending, TlsTestPending> alreadyProcessingFilter =
                new TransformManyBlock<TlsTestPending, TlsTestPending>(_ => ApplyFilter(_), preBatchBlockOptions);

            TransformBlock<TlsTestPending, MxHostTestDetails> mxTestProcessorQueue =
                new TransformBlock<TlsTestPending, MxHostTestDetails>(async _ => await TestMxHost(_), parallelPreBatchBlockOptions);

            BatchBlock<MxHostTestDetails> toPublishQueue =
                new BatchBlock<MxHostTestDetails>(_config.PublishBatchSize, postBatchGroupingBlockOptions);

            Timer timer = new Timer(_ =>
            {
                toPublishQueue.TriggerBatch();
                _log.LogDebug("Batch triggered.");
            });

            TransformBlock<MxHostTestDetails, MxHostTestDetails> batchTimeoutQueue =
                new TransformBlock<MxHostTestDetails, MxHostTestDetails>(_ => ResetTimer(_, timer), preBatchBlockOptions);

            TransformBlock<MxHostTestDetails[], MxHostTestDetails[]> mxResultsPublisher =
                new TransformBlock<MxHostTestDetails[], MxHostTestDetails[]>(async _ => await PublishResults(_), postBatchBlockOptions);

            ActionBlock<MxHostTestDetails[]> filterItemRemover =
                new ActionBlock<MxHostTestDetails[]>(async _ => await RemoveFromQueue(_), postBatchBlockOptions);

            pollQueue.LinkTo(mxHostToProcessQueue, linkOptions, hosts => hosts != null);
            mxHostToProcessQueue.LinkTo(alreadyProcessingFilter, linkOptions, result => result != null);
            alreadyProcessingFilter.LinkTo(mxTestProcessorQueue, linkOptions, result => result != null);
            mxTestProcessorQueue.LinkTo(batchTimeoutQueue, linkOptions);
            batchTimeoutQueue.LinkTo(toPublishQueue, linkOptions);
            toPublishQueue.LinkTo(mxResultsPublisher, linkOptions);
            mxResultsPublisher.LinkTo(filterItemRemover, linkOptions);

            await Task.WhenAll(StartPipeline(pollQueue, cancellationToken),
                PrintStats(mxTestProcessorQueue, cancellationToken),
                pollQueue.Completion,
                mxHostToProcessQueue.Completion,
                mxTestProcessorQueue.Completion,
                batchTimeoutQueue.Completion,
                toPublishQueue.Completion,
                mxResultsPublisher.Completion,
                filterItemRemover.Completion);
        }

        private async Task RemoveFromQueue(MxHostTestDetails[] messages)
        {
            foreach (MxHostTestDetails tlsTestResult in messages)
            {
                _processingFilter.RemoveFilter(tlsTestResult.TestResults.Id);

                await DeleteMessageFromQueue(tlsTestResult.TestResults.Id, tlsTestResult.MessageId,
                    tlsTestResult.ReceiptHandle);
            }
        }

        private async Task<TlsTestPending[]> GetMxHostToProcess()
        {
            List<TlsTestPending> hosts = new List<TlsTestPending>();

            try
            {
                _log.LogDebug("Getting mx hosts to process.");

                hosts.AddRange( await _mxQueueProcessor.GetMxHosts());

                _log.LogDebug(hosts.Any()
                    ? $"Found {hosts.Count} mx hosts to test: {Environment.NewLine}{string.Join(Environment.NewLine, hosts.Select(_ => _.Id))}"
                    : "Didn't find any hosts to test.");

                return hosts.ToArray();
            }
            catch (Exception e)
            {
                _log.LogError($"The following error occured fetching mx hosts to test:: {e.Message}{Environment.NewLine}{e.StackTrace}");
            }

            return hosts.ToArray();
        }

        private TlsTestPending[] ApplyFilter(TlsTestPending testPending)
        {
            try
            {
                _log.LogDebug("Applying filter.");

                TlsTestPending filteredResult = _processingFilter.ApplyFilter(testPending);

                _log.LogDebug($"Already being processed: {filteredResult == null}");

                if (filteredResult != null) return new[] {filteredResult};
            }
            catch (Exception e)
            {
                _log.LogError(
                    $"The following error occured applying filter (host: {testPending.Id}): {e.Message}{Environment.NewLine}{e.StackTrace}");
            }

            return new TlsTestPending[0];
        }

        private async Task<MxHostTestDetails> TestMxHost(TlsTestPending tlsTest)
        {
            try
            {
                _log.LogDebug($"Testing smtp for host: {tlsTest.Id}");

                TlsTestResults tlsTestResults = await _mxHostTester.Test(tlsTest);

                _log.LogDebug($"Testing completed for host: {tlsTest.Id}");

                return new MxHostTestDetails(tlsTestResults, tlsTest.MessageId, tlsTest.ReceiptHandle);
            }
            catch (Exception e)
            {
                _log.LogError(
                    $"The following error occured testing host: {tlsTest.Id}): {e.Message}{Environment.NewLine}{e.StackTrace}");
                throw;
            }
        }

        private async Task<MxHostTestDetails[]> PublishResults(MxHostTestDetails[] tlsTestResults)
        {
            foreach (MxHostTestDetails tlsTestResult in tlsTestResults)
            {
                _log.LogDebug($"Publishing smtp test results for host: {tlsTestResult.TestResults.Id}");
                await _publisher.Publish(tlsTestResult.TestResults, _config.SnsTopicArn);
                _log.LogDebug($"Published smtp test results for host: {tlsTestResult.TestResults.Id}");
            }

            return tlsTestResults;
        }

        private MxHostTestDetails ResetTimer(MxHostTestDetails tlsTestResults, Timer timer)
        {
            try
            {
                _log.LogDebug("Batch timeout timer reset");
                timer.Change(_config.PublishBatchFlushIntervalSeconds, Timeout.Infinite);
            }
            catch (Exception e)
            {
                _log.LogError($"The following error occured resetting batch timeout timer: {e.Message}{Environment.NewLine}{e.StackTrace}");
            }

            return tlsTestResults;
        }

        private async Task PrintStats(TransformBlock<TlsTestPending, MxHostTestDetails> processorBlock, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                _log.LogDebug($"Mx tester is processing {_processingFilter.HostCount} items and has {processorBlock.InputCount} waiting to be processed.");
                
                Task delay = Task.Delay(TimeSpan.FromSeconds(60));
                await Task.WhenAny(delay, cancellationToken.WhenCanceled());
            }
        }

        private async Task DeleteMessageFromQueue(string host, string messageId, string receiptHandle)
        {
            _log.LogDebug(
                $"Deleting message from sqs for host: {host} - Message Id: {messageId}");
            await _mxQueueProcessor.DeleteMessage(messageId, receiptHandle);
            _log.LogDebug(
                $"Deleted message from sqs for host: {host} - Message Id: {messageId}");
        }

        private async Task StartPipeline(ITargetBlock<object> inputQueue,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                while (!await inputQueue.SendAsync(new object()))
                {
                    _log.LogWarning("Waiting to schedule poll...");
                    await Task.Delay(500);
                }

                _log.LogDebug("Poll scheduled.");

                Task delay = Task.Delay(TimeSpan.FromSeconds(_config.SchedulerRunIntervalSeconds));
                await Task.WhenAny(delay, cancellationToken.WhenCanceled());

            }
            inputQueue.Complete();
        }
    }
}
