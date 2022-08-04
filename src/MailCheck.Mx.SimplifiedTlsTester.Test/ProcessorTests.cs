using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using MailCheck.Common.Contracts.Advisories;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Mx.Contracts.Simplified;
using MailCheck.Mx.SimplifiedTlsTester.Domain;
using MailCheck.Mx.SimplifiedTlsTester.TestRunner;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace MailCheck.Mx.SimplifiedTlsTester.Test
{
    public class ProcessorTests
    {
        private Processor _processor;
        private ISqsClient _sqsClient;
        private ITestRunner _testRunner;
        private IMessagePublisher _publisher;
        private IProcessorConfig _testerConfig;

        [SetUp]
        public void SetUp()
        {
            _sqsClient = A.Fake<ISqsClient>();
            _testRunner = A.Fake<ITestRunner>();
            _publisher = A.Fake<IMessagePublisher>();
            _testerConfig = A.Fake<IProcessorConfig>();
            _processor = new Processor(_sqsClient, _testRunner, _publisher, _testerConfig, A.Fake<ILogger<Processor>>());
        }

        [Test]
        public async Task ProcessOrchestratesDequeuingTestingAndDeletingMessages()
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            A.CallTo(() => _sqsClient.GetTestsPending(cancellationTokenSource.Token))
                .Invokes(() => cancellationTokenSource.Cancel())
                .Returns(new List<SimplifiedTlsTestPending> { new SimplifiedTlsTestPending("testIpAddress1") });

            A.CallTo(() => _testRunner.Run("testIpAddress1")).Returns(new SimplifiedTlsTestResults("testIpAddress1"));

            await _processor.Process(cancellationTokenSource.Token);

            A.CallTo(() => _publisher.Publish(A<SimplifiedTlsTestResults>.That.Matches(x => x.Id == "testIpAddress1"), A<string>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _sqsClient.DeleteMessages(A<List<SimplifiedTlsTestPending>>.That.Matches(x => x[0].Id == "testIpAddress1"))).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task ProcessHandlesEmptyQueue()
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            A.CallTo(() => _sqsClient.GetTestsPending(cancellationTokenSource.Token))
                .Invokes(() => cancellationTokenSource.Cancel())
                .Returns(new List<SimplifiedTlsTestPending>());

            await _processor.Process(cancellationTokenSource.Token);
            A.CallTo(() => _testRunner.Run("testIpAddress1")).MustNotHaveHappened();
            A.CallTo(() => _publisher.Publish(A<SimplifiedTlsTestResults>._, A<string>._)).MustNotHaveHappened();
            A.CallTo(() => _sqsClient.DeleteMessages(A<List<SimplifiedTlsTestPending>>._)).MustNotHaveHappened();
        }

        [Test]
        public async Task ProcessHandlesErrorInTestRunner()
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            A.CallTo(() => _sqsClient.GetTestsPending(cancellationTokenSource.Token))
                .Invokes(() => cancellationTokenSource.Cancel())
                .Returns(new List<SimplifiedTlsTestPending> { new SimplifiedTlsTestPending("testIpAddress1") });
            A.CallTo(() => _testRunner.Run("testIpAddress1")).Throws<Exception>();

            await _processor.Process(cancellationTokenSource.Token);

            A.CallTo(() => _publisher.Publish(A<SimplifiedTlsTestResults>._, A<string>._)).MustNotHaveHappened();
            A.CallTo(() => _sqsClient.DeleteMessages(A<List<SimplifiedTlsTestPending>>.That.Matches(x => x[0].Id == "testIpAddress1"))).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task ProcessShouldTimeoutLongRunningTests()
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            A.CallTo(() => _sqsClient.GetTestsPending(cancellationTokenSource.Token))
                .Invokes(() => cancellationTokenSource.Cancel())
                .Returns(new List<SimplifiedTlsTestPending> { new SimplifiedTlsTestPending("testIpAddress1"), new SimplifiedTlsTestPending("testIpAddress2") });

            _processor.TimeoutTaskOverride = Task.CompletedTask;
            A.CallTo(() => _testRunner.Run("testIpAddress1")).Returns(new SimplifiedTlsTestResults("testIpAddress1"));
            A.CallTo(() => _testRunner.Run("testIpAddress2")).Returns(new TaskCompletionSource<SimplifiedTlsTestResults>().Task);

            await _processor.Process(cancellationTokenSource.Token);

            A.CallTo(() => _publisher.Publish(A<SimplifiedTlsTestResults>.That.Matches(x => x.Id == "testIpAddress1"), A<string>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _publisher.Publish(A<SimplifiedTlsTestResults>.That.Matches(x => x.Id == "testIpAddress2"), A<string>._)).MustNotHaveHappened();
            A.CallTo(() => _sqsClient.DeleteMessages(A<List<SimplifiedTlsTestPending>>.That.Matches(x => x[0].Id == "testIpAddress1" && x[1].Id == "testIpAddress2"))).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task ProcessDoesNotPublishInconclusiveResults()
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            A.CallTo(() => _sqsClient.GetTestsPending(cancellationTokenSource.Token))
                .Invokes(() => cancellationTokenSource.Cancel())
                .Returns(new List<SimplifiedTlsTestPending> { new SimplifiedTlsTestPending("testIpAddress1") });
            A.CallTo(() => _testRunner.Run("testIpAddress1")).Returns(new SimplifiedTlsTestResults("testIpAddress1") { Inconclusive = true });

            await _processor.Process(cancellationTokenSource.Token);

            A.CallTo(() => _publisher.Publish(A<SimplifiedTlsTestResults>._, A<string>._)).MustNotHaveHappened();
            A.CallTo(() => _sqsClient.DeleteMessages(A<List<SimplifiedTlsTestPending>>.That.Matches(x => x[0].Id == "testIpAddress1"))).MustHaveHappenedOnceExactly();
        }
    }
}
