using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
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
            
            _mxSecurityTesterProcessor = new MxSecurityTesterProcessor(_mxQueueProcessor,
                _publisher,
                _mxHostTester,
                _mxSecurityTesterConfig,
                _processingFilter,
                _log);
        }

        [Test]
        public async Task NoProfileToProcessNoProcessingOccurs()
        {
            A.CallTo(() => _mxQueueProcessor.GetMxHosts())
                .Returns(Task.FromResult(new List<TlsTestPending>()));
            A.CallTo(() => _mxSecurityTesterConfig.SchedulerRunIntervalSeconds).Returns(10);

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            Task process = _mxSecurityTesterProcessor.Process(cancellationTokenSource.Token);

            cancellationTokenSource.Cancel();

            await process;

            A.CallTo(() => _mxQueueProcessor.GetMxHosts()).MustHaveHappenedOnceExactly();
            A.CallTo(() => _mxHostTester.Test(A<TlsTestPending>._)).MustNotHaveHappened();
            A.CallTo(() => _publisher.Publish(A<Message>._, A<string>._, A<string>._)).MustNotHaveHappened();
            A.CallTo(() => _mxQueueProcessor.DeleteMessage(A<string>._, A<string>._)).MustNotHaveHappened();
            A.CallTo(() => _processingFilter.ApplyFilter(A<TlsTestPending>._)).MustNotHaveHappened();
            A.CallTo(() => _processingFilter.RemoveFilter(A<string>._)).MustNotHaveHappened();
        }

        [Test]
        public async Task HostsAreProcessed()
        {
            List<TlsTestPending> list = new List<TlsTestPending>
            {
                CreateMxHostTestPending(),
            };

            A.CallTo(() => _mxQueueProcessor.GetMxHosts()).Returns(Task.FromResult(list));

            A.CallTo(() => _mxSecurityTesterConfig.SchedulerRunIntervalSeconds).Returns(10);
            A.CallTo(() => _mxSecurityTesterConfig.PublishBatchFlushIntervalSeconds).Returns(1);
            A.CallTo(() => _mxSecurityTesterConfig.PublishBatchSize).Returns(1);
            A.CallTo(() => _mxSecurityTesterConfig.SchedulerRunIntervalSeconds).Returns(10);


            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            Task process = _mxSecurityTesterProcessor.Process(cancellationTokenSource.Token);

            cancellationTokenSource.Cancel();

            await process;

            A.CallTo(() => _mxQueueProcessor.GetMxHosts()).MustHaveHappenedOnceExactly();
            A.CallTo(() => _mxHostTester.Test(A<TlsTestPending>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _publisher.Publish(A<Message>._, A<string>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _mxQueueProcessor.DeleteMessage(A<string>._, A<string>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _processingFilter.ApplyFilter(A<TlsTestPending>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _processingFilter.RemoveFilter(A<string>._)).MustHaveHappenedOnceExactly();
        }


        private TlsTestPending CreateMxHostTestPending()
        {
            return new TlsTestPending("host.abc.gov.uk");
        }
    }
}
