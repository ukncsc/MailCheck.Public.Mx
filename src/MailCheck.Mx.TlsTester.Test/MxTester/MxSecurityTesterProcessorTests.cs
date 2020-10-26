using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using FakeItEasy;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Mx.Contracts.Tester;
using MailCheck.Mx.TlsTester.Config;
using MailCheck.Mx.TlsTester.Domain;
using MailCheck.Mx.TlsTester.MxTester;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace MailCheck.Mx.TlsTester.Test.MxTester
{
    [TestFixture]
    public class MxSecurityTesterProcessorTests
    {
        private MxSecurityTesterProcessor _mxSecurityTesterProcessor;
        private IMxQueueProcessor _mxQueueProcessor;
        private IMessagePublisher _publisher;
        private ITlsSecurityTesterAdapator _mxHostTester;
        private IMxTesterConfig _mxSecurityTesterConfig;
        private IMxSecurityProcessingFilter _processingFilter;
        private ILogger<MxSecurityTesterProcessor> _log;

        private ITargetBlock<object> _pipelineStartBlock;
        private CancellationTokenSource cancellationTokenSource;
        private Task cancellationTokenSourceTask;

        [SetUp]
        public void SetUp()
        {
            _mxQueueProcessor = A.Fake<IMxQueueProcessor>();
            _publisher = A.Fake<IMessagePublisher>();
            _mxHostTester = A.Fake<ITlsSecurityTesterAdapator>();
            _mxSecurityTesterConfig = A.Fake<IMxTesterConfig>();
            _processingFilter = A.Fake<IMxSecurityProcessingFilter>();
            _log = A.Fake<ILogger<MxSecurityTesterProcessor>>();

            A.CallTo(() => _mxSecurityTesterConfig.PublishBatchSize).Returns(1);
            A.CallTo(() => _mxSecurityTesterConfig.PublishBatchFlushIntervalSeconds).Returns(1);
            A.CallTo(() => _mxSecurityTesterConfig.TlsTesterThreadCount).Returns(1);

            cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSourceTask = cancellationTokenSource.Token.WhenCanceled();

            _mxSecurityTesterProcessor = new MxSecurityTesterProcessor(_mxQueueProcessor,
                _publisher,
                _mxHostTester,
                _mxSecurityTesterConfig,
                _processingFilter,
                _log,
                RunPipelineDelegate);
        }

        private Task RunPipelineDelegate(ITargetBlock<object> inputBlock, CancellationToken cancelationToken)
        {
            _pipelineStartBlock = inputBlock;
            return cancellationTokenSourceTask;
        }

        [Test]
        public async Task NoProfileToProcessNoProcessingOccurs()
        {
            A.CallTo(() => _mxQueueProcessor.GetMxHosts())
                .Returns(Task.FromResult(new List<TlsTestPending>()));

            Task process = _mxSecurityTesterProcessor.Process(cancellationTokenSource.Token);

            await _pipelineStartBlock.SendAsync(null);

            cancellationTokenSource.Cancel();

            await process;

            A.CallTo(() => _mxQueueProcessor.GetMxHosts()).MustHaveHappenedOnceExactly();
            A.CallTo(() => _mxHostTester.Test(A<TlsTestPending>._)).MustNotHaveHappened();
            A.CallTo(() => _publisher.Publish(A<Message>._, A<string>._)).MustNotHaveHappened();
            A.CallTo(() => _mxQueueProcessor.DeleteMessage(A<string>._, A<string>._)).MustNotHaveHappened();
            A.CallTo(() => _processingFilter.Reserve(A<string>._)).MustNotHaveHappened();
            A.CallTo(() => _processingFilter.ReleaseReservation(A<string>._)).MustNotHaveHappened();
        }

        [Test]
        public async Task HostsAreProcessed()
        {
            var testPending = CreateMxHostTestPending();
            List<TlsTestPending> list = new List<TlsTestPending>
            {
                testPending
            };

            A.CallTo(() => _mxQueueProcessor.GetMxHosts())
                .Returns(Task.FromResult(list)).Once()
                .Then
                .Returns(Task.FromResult(new List<TlsTestPending>()));
            A.CallTo(() => _mxQueueProcessor.DeleteMessage(A<string>._, A<string>._)).Returns(Task.CompletedTask);

            var testResult = CreateMxHostTestResult();
            A.CallTo(() => _mxHostTester.Test(testPending)).Returns(Task.FromResult(testResult)).Once();

            A.CallTo(() => _processingFilter.Reserve(A<string>._)).Returns(true);

            A.CallTo(() => _publisher.Publish(A<Message>._, A<string>._)).Returns(Task.CompletedTask);
            
            Task process = _mxSecurityTesterProcessor.Process(cancellationTokenSource.Token);

            await _pipelineStartBlock.SendAsync(null);

            cancellationTokenSource.Cancel();

            await process;

            A.CallTo(() => _mxQueueProcessor.GetMxHosts()).MustHaveHappenedOnceExactly();
            A.CallTo(() => _mxHostTester.Test(testPending)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _publisher.Publish(testResult, A<string>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _mxQueueProcessor.DeleteMessage("MessageId1", "ReceiptHandle1")).MustHaveHappenedOnceExactly();
            A.CallTo(() => _processingFilter.Reserve("host.domain1.gov.uk")).MustHaveHappenedOnceExactly();
            A.CallTo(() => _processingFilter.ReleaseReservation("host.domain1.gov.uk")).MustHaveHappenedOnceExactly();
        }


        [Test]
        public async Task DuplicateHostToProcessNoProcessingOccurs()
        {
            var testPending = CreateMxHostTestPending();
            List<TlsTestPending> list = new List<TlsTestPending>
            {
                testPending
            };

            A.CallTo(() => _mxQueueProcessor.GetMxHosts())
                .Returns(Task.FromResult(list)).Once()
                .Then
                .Returns(Task.FromResult(new List<TlsTestPending>()));

            A.CallTo(() => _processingFilter.Reserve(A<string>._)).Returns(false);

            Task process = _mxSecurityTesterProcessor.Process(cancellationTokenSource.Token);

            await _pipelineStartBlock.SendAsync(null);

            cancellationTokenSource.Cancel();

            await process;

            A.CallTo(() => _mxQueueProcessor.GetMxHosts()).MustHaveHappenedOnceExactly();
            A.CallTo(() => _processingFilter.Reserve("host.domain1.gov.uk")).MustHaveHappenedOnceExactly();

            A.CallTo(() => _mxHostTester.Test(A<TlsTestPending>._)).MustNotHaveHappened();
            A.CallTo(() => _publisher.Publish(A<Message>._, A<string>._)).MustNotHaveHappened();
            A.CallTo(() => _mxQueueProcessor.DeleteMessage(A<string>._, A<string>._)).MustNotHaveHappened();
            A.CallTo(() => _processingFilter.ReleaseReservation(A<string>._)).MustNotHaveHappened();
        }

        [Test]
        public async Task MultipleHostsAreProcessed()
        {
            var testPending = CreateMxHostTestPending(1);
            var testPending2 = CreateMxHostTestPending(2);
            var testPending3 = CreateMxHostTestPending(3);

            List<TlsTestPending> firstHosts = new List<TlsTestPending>
            {
                testPending,
                testPending2,
                testPending3
            };

            A.CallTo(() => _mxQueueProcessor.GetMxHosts())
                .Returns(Task.FromResult(firstHosts)).Once()
                .Then
                .Returns(Task.FromResult(new List<TlsTestPending>()));

            A.CallTo(() => _mxQueueProcessor.DeleteMessage(A<string>._, A<string>._)).Returns(Task.CompletedTask);

            var testResult = CreateMxHostTestResult();
            A.CallTo(() => _mxHostTester.Test(A<TlsTestPending>._)).Returns(Task.FromResult(testResult));

            A.CallTo(() => _processingFilter.Reserve(A<string>._)).Returns(true);

            A.CallTo(() => _publisher.Publish(A<Message>._, A<string>._)).Returns(Task.CompletedTask);

            Task process = _mxSecurityTesterProcessor.Process(cancellationTokenSource.Token);

            await _pipelineStartBlock.SendAsync(null);

            cancellationTokenSource.Cancel();

            await process;

            A.CallTo(() => _mxQueueProcessor.GetMxHosts()).MustHaveHappenedOnceExactly();
            A.CallTo(() => _mxHostTester.Test(A<TlsTestPending>._)).MustHaveHappened(3, Times.Exactly);
            A.CallTo(() => _publisher.Publish(A<Message>._, A<string>._)).MustHaveHappened(3, Times.Exactly);
            A.CallTo(() => _mxQueueProcessor.DeleteMessage(A<string>._, A<string>._)).MustHaveHappened(3, Times.Exactly);
            A.CallTo(() => _processingFilter.Reserve(A<string>._)).MustHaveHappened(3, Times.Exactly);
            A.CallTo(() => _processingFilter.ReleaseReservation(A<string>._)).MustHaveHappened(3, Times.Exactly);
        }

        [Test]
        public async Task MultipleHostsWithMultipleProcessorsAreProcessed()
        {
            A.CallTo(() => _mxSecurityTesterConfig.TlsTesterThreadCount)
                .Returns(2);

            _mxSecurityTesterProcessor = new MxSecurityTesterProcessor(_mxQueueProcessor,
                _publisher,
                _mxHostTester,
                _mxSecurityTesterConfig,
                _processingFilter,
                _log,
                RunPipelineDelegate);

            var testPending = CreateMxHostTestPending(1);
            var testPending2 = CreateMxHostTestPending(2);

            List<TlsTestPending> firstHosts = new List<TlsTestPending>
            {
                testPending,
                testPending2
            };

            A.CallTo(() => _mxQueueProcessor.GetMxHosts())
                .Returns(Task.FromResult(firstHosts)).Once()
                .Then
                .Returns(Task.FromResult(new List<TlsTestPending>()));

            A.CallTo(() => _mxQueueProcessor.DeleteMessage(A<string>._, A<string>._)).Returns(Task.CompletedTask);

            var testResult = CreateMxHostTestResult();
            A.CallTo(() => _mxHostTester.Test(A<TlsTestPending>._)).Returns(Task.FromResult(testResult));

            A.CallTo(() => _processingFilter.Reserve(A<string>._)).Returns(true);

            A.CallTo(() => _publisher.Publish(A<Message>._, A<string>._)).Returns(Task.CompletedTask);

            Task process = _mxSecurityTesterProcessor.Process(cancellationTokenSource.Token);

            await _pipelineStartBlock.SendAsync(null);

            cancellationTokenSource.Cancel();

            await process;

            A.CallTo(() => _mxQueueProcessor.GetMxHosts()).MustHaveHappenedOnceExactly();
            A.CallTo(() => _mxHostTester.Test(A<TlsTestPending>._)).MustHaveHappened(2, Times.Exactly);
            A.CallTo(() => _publisher.Publish(A<Message>._, A<string>._)).MustHaveHappened(2, Times.Exactly);
            A.CallTo(() => _mxQueueProcessor.DeleteMessage(A<string>._, A<string>._)).MustHaveHappened(2, Times.Exactly);
            A.CallTo(() => _processingFilter.Reserve(A<string>._)).MustHaveHappened(2, Times.Exactly);
            A.CallTo(() => _processingFilter.ReleaseReservation(A<string>._)).MustHaveHappened(2, Times.Exactly);
        }

        [Test]
        public async Task ExceptionOnTestDoesntPublishAndReleasesReservation()
        {
            var testPending = CreateMxHostTestPending();
            List<TlsTestPending> list = new List<TlsTestPending>
            {
                testPending
            };

            A.CallTo(() => _mxQueueProcessor.GetMxHosts())
                .Returns(Task.FromResult(list)).Once()
                .Then
                .Returns(Task.FromResult(new List<TlsTestPending>()));
            A.CallTo(() => _mxQueueProcessor.DeleteMessage(A<string>._, A<string>._)).Returns(Task.CompletedTask);

            A.CallTo(() => _mxHostTester.Test(testPending)).Throws<Exception>();

            A.CallTo(() => _processingFilter.Reserve(A<string>._)).Returns(true);

            A.CallTo(() => _publisher.Publish(A<Message>._, A<string>._)).Returns(Task.CompletedTask);

            Task process = _mxSecurityTesterProcessor.Process(cancellationTokenSource.Token);

            await _pipelineStartBlock.SendAsync(null);

            cancellationTokenSource.Cancel();

            await process;

            A.CallTo(() => _mxQueueProcessor.GetMxHosts()).MustHaveHappenedOnceExactly();
            A.CallTo(() => _mxHostTester.Test(testPending)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _processingFilter.Reserve("host.domain1.gov.uk")).MustHaveHappenedOnceExactly();
            A.CallTo(() => _processingFilter.ReleaseReservation("host.domain1.gov.uk")).MustHaveHappenedOnceExactly();
            A.CallTo(() => _publisher.Publish(A<Message>._, A<string>._)).MustNotHaveHappened();
            A.CallTo(() => _mxQueueProcessor.DeleteMessage(A<string>._, A<string>._)).MustNotHaveHappened();
            A.CallTo(() => _processingFilter.ReleaseReservation("host.domain1.gov.uk")).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task ExceptionOnPublishReleasesReservation()
        {
            var testPending = CreateMxHostTestPending();
            List<TlsTestPending> list = new List<TlsTestPending>
            {
                testPending
            };

            A.CallTo(() => _mxQueueProcessor.GetMxHosts())
                .Returns(Task.FromResult(list)).Once()
                .Then
                .Returns(Task.FromResult(new List<TlsTestPending>()));
            A.CallTo(() => _mxQueueProcessor.DeleteMessage(A<string>._, A<string>._)).Returns(Task.CompletedTask);

            var testResult = CreateMxHostTestResult();
            A.CallTo(() => _mxHostTester.Test(testPending)).Returns(Task.FromResult(testResult)).Once();

            A.CallTo(() => _processingFilter.Reserve(A<string>._)).Returns(true);

            A.CallTo(() => _publisher.Publish(testResult, A<string>._)).Throws<Exception>();

            Task process = _mxSecurityTesterProcessor.Process(cancellationTokenSource.Token);

            await _pipelineStartBlock.SendAsync(null);

            cancellationTokenSource.Cancel();

            await process;

            A.CallTo(() => _mxQueueProcessor.GetMxHosts()).MustHaveHappenedOnceExactly();
            A.CallTo(() => _mxHostTester.Test(testPending)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _processingFilter.Reserve("host.domain1.gov.uk")).MustHaveHappenedOnceExactly();
            A.CallTo(() => _processingFilter.ReleaseReservation("host.domain1.gov.uk")).MustHaveHappenedOnceExactly();
            A.CallTo(() => _publisher.Publish(testResult, A<string>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _mxQueueProcessor.DeleteMessage(A<string>._, A<string>._)).MustNotHaveHappened();
            A.CallTo(() => _processingFilter.ReleaseReservation("host.domain1.gov.uk")).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task ExceptionOnDeleteMessageReleasesReservation()
        {
            var testPending = CreateMxHostTestPending();
            List<TlsTestPending> list = new List<TlsTestPending>
            {
                testPending
            };

            A.CallTo(() => _mxQueueProcessor.GetMxHosts())
                .Returns(Task.FromResult(list)).Once()
                .Then
                .Returns(Task.FromResult(new List<TlsTestPending>()));
            
            A.CallTo(() => _mxQueueProcessor.DeleteMessage(A<string>._, A<string>._)).Throws<Exception>();

            var testResult = CreateMxHostTestResult();
            A.CallTo(() => _mxHostTester.Test(testPending)).Returns(Task.FromResult(testResult)).Once();

            A.CallTo(() => _processingFilter.Reserve(A<string>._)).Returns(true);

            A.CallTo(() => _publisher.Publish(A<Message>._, A<string>._)).Returns(Task.CompletedTask);

            Task process = _mxSecurityTesterProcessor.Process(cancellationTokenSource.Token);

            await _pipelineStartBlock.SendAsync(null);

            cancellationTokenSource.Cancel();

            await process;

            A.CallTo(() => _mxQueueProcessor.GetMxHosts()).MustHaveHappenedOnceExactly();
            A.CallTo(() => _mxHostTester.Test(testPending)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _processingFilter.Reserve("host.domain1.gov.uk")).MustHaveHappenedOnceExactly();
            A.CallTo(() => _processingFilter.ReleaseReservation("host.domain1.gov.uk")).MustHaveHappenedOnceExactly();
            A.CallTo(() => _publisher.Publish(testResult, A<string>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _mxQueueProcessor.DeleteMessage(A<string>._, A<string>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _processingFilter.ReleaseReservation("host.domain1.gov.uk")).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task ExceptionOnQueuePoll()
        {
            var testPending = CreateMxHostTestPending();
            List<TlsTestPending> list = new List<TlsTestPending>
            {
                testPending
            };

            A.CallTo(() => _mxQueueProcessor.GetMxHosts())
                .Throws<Exception>().Once()
                .Then
                .Returns(Task.FromResult(list)).Once()
                .Then
                .Returns(Task.FromResult(new List<TlsTestPending>()));

            A.CallTo(() => _mxQueueProcessor.DeleteMessage(A<string>._, A<string>._)).Returns(Task.CompletedTask);

            var testResult = CreateMxHostTestResult();
            A.CallTo(() => _mxHostTester.Test(testPending)).Returns(Task.FromResult(testResult)).Once();

            A.CallTo(() => _processingFilter.Reserve(A<string>._)).Returns(true);

            A.CallTo(() => _publisher.Publish(A<Message>._, A<string>._)).Returns(Task.CompletedTask);

            Task process = _mxSecurityTesterProcessor.Process(cancellationTokenSource.Token);

            await _pipelineStartBlock.SendAsync(null);
            await _pipelineStartBlock.SendAsync(null);
            
            cancellationTokenSource.Cancel();

            await process;

            A.CallTo(() => _mxQueueProcessor.GetMxHosts()).MustHaveHappenedTwiceExactly();
            A.CallTo(() => _mxHostTester.Test(testPending)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _publisher.Publish(testResult, A<string>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _mxQueueProcessor.DeleteMessage("MessageId1", "ReceiptHandle1")).MustHaveHappenedOnceExactly();
            A.CallTo(() => _processingFilter.Reserve("host.domain1.gov.uk")).MustHaveHappenedOnceExactly();
            A.CallTo(() => _processingFilter.ReleaseReservation("host.domain1.gov.uk")).MustHaveHappenedOnceExactly();
        }


        private TlsTestPending CreateMxHostTestPending(int num = 1)
        {
            return new TlsTestPending($"host.domain{num}.gov.uk") { MessageId = $"MessageId{num}", ReceiptHandle = $"ReceiptHandle{num}" };
        }

        private TlsTestResults CreateMxHostTestResult()
        {
            return new TlsTestResults(
                "host.abc.gov.uk",
                false,
                false,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null
            );
        }
    }
}
