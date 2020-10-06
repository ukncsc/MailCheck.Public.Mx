using System;
using System.Threading.Tasks;
using FakeItEasy;
using MailCheck.Common.Contracts.Messaging;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Common.Messaging.Common.Exception;
using MailCheck.Common.Util;
using MailCheck.Mx.Contracts.Entity;
using MailCheck.Mx.Contracts.External;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.TlsEntity;
using MailCheck.Mx.Contracts.TlsEvaluator;
using MailCheck.Mx.TlsEntity.Config;
using MailCheck.Mx.TlsEntity.Dao;
using MailCheck.Mx.TlsEntity.Entity;
using MailCheck.Mx.TlsEntity.Entity.DomainStatus;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace MailCheck.Mx.TlsEntity.Test
{
    [TestFixture]
    public class TlsEntityTests
    {
        private TlsEntity.Entity.TlsEntity _tlsEntity;
        private ITlsEntityDao _dao;
        private ITlsEntityConfig _tlsEntityConfig;
        private IMessageDispatcher _dispatcher;
        private IDomainStatusPublisher _domainStatusPublisher;
        private IClock _clock;
        private ILogger<TlsEntity.Entity.TlsEntity> _log;

        [SetUp]
        public void SetUp()
        {
            _dao = A.Fake<ITlsEntityDao>();
            _log = A.Fake<ILogger<TlsEntity.Entity.TlsEntity>>();
            _clock = A.Fake<IClock>();
            _tlsEntityConfig = A.Fake<ITlsEntityConfig>();
            _dispatcher = A.Fake<IMessageDispatcher>();
            _domainStatusPublisher = A.Fake<IDomainStatusPublisher>();

            _tlsEntity = new TlsEntity.Entity.TlsEntity(_dao, _clock, _tlsEntityConfig, _dispatcher, _domainStatusPublisher, _log);
        }
        
        [Test]
        public async Task ShouldDispatchPollPendingWhenScheduledReminderReceived()
        {
            string hostName = "testhostname";
            string snsTopicArn = "snsTopicArn";
            TlsEntityState stateFromDb = new TlsEntityState(hostName) { TlsState = TlsState.Created };

            A.CallTo(() => _dao.Get(hostName)).Returns(stateFromDb);
            A.CallTo(() => _tlsEntityConfig.SnsTopicArn).Returns(snsTopicArn);

            await _tlsEntity.Handle(new TlsScheduledReminder(Guid.NewGuid().ToString(), hostName));

            A.CallTo(() => _dispatcher.Dispatch(A<TlsTestPending>.That.Matches(entity => entity.Id == hostName),
              snsTopicArn)).MustHaveHappenedOnceExactly();
            Assert.AreEqual(stateFromDb.TlsState, TlsState.PollPending);
        }

        [Test]
        public async Task ShouldRescheduledWhenItHasFailedToCorrectPeriod()
        {
            string snsTopicArn = "snsTopicArn";
            A.CallTo(() => _tlsEntityConfig.SnsTopicArn).Returns(snsTopicArn);

            string hostName = "testhostname";

            TlsEntityState stateFromDb = new TlsEntityState(hostName) { TlsState = TlsState.Created };
            A.CallTo(() => _dao.Get(hostName)).Returns(Task.FromResult(stateFromDb));


            A.CallTo(() => _tlsEntityConfig.FailureNextScheduledInSeconds).Returns(15);
            A.CallTo(() => _tlsEntityConfig.MaxTlsRetryAttempts).Returns(5);
            
            A.CallTo(() => _clock.GetDateTimeUtc()).Returns(DateTime.MinValue);
            
            CertificateResults certificateResults = new CertificateResults(null, null);

            TlsResultsEvaluated message = new TlsResultsEvaluated(hostName, true, new TlsRecords(
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS))
                , new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS))
                , new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS))
                , new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS))
            ), certificateResults);

            await _tlsEntity.Handle(message);

            A.CallTo(() => _dao.Save(stateFromDb)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _dispatcher.Dispatch(A<TlsRecordEvaluationsChanged>.That.Matches(a => a.Id == message.Id && a.TlsRecords == message.TlsRecords && a.CertificateResults == message.Certificates), snsTopicArn, null)).MustNotHaveHappened();
            A.CallTo(() => _dispatcher.Dispatch(A<CreateScheduledReminder>.That.Matches(a => a.ResourceId == hostName && a.Service == "Tls" && a.ScheduledTime.Second == 15), snsTopicArn)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task ShouldHandleChangeSaveAndDispatchWhenTlsTestResultIsReceived()
        {
            string snsTopicArn = "snsTopicArn";
            A.CallTo(() => _tlsEntityConfig.SnsTopicArn).Returns(snsTopicArn);

            string hostName = "testhostname";

            TlsEntityState stateFromDb = new TlsEntityState(hostName) { TlsState = TlsState.Created };
            A.CallTo(() => _dao.Get(hostName)).Returns(Task.FromResult(stateFromDb));


            A.CallTo(() => _tlsEntityConfig.NextScheduledInSeconds).Returns(33);
            A.CallTo(() => _clock.GetDateTimeUtc()).Returns(DateTime.MinValue);
            
            CertificateResults certificateResults = new CertificateResults(null, null);

            TlsResultsEvaluated message = new TlsResultsEvaluated(hostName, false, new TlsRecords(
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS))
                , new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS))
                , new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS))
                , new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS))
            ), certificateResults);

            await _tlsEntity.Handle(message);

            Assert.AreEqual(stateFromDb.TlsState, TlsState.Evaluated);
            Assert.AreEqual(stateFromDb.TlsRecords, message.TlsRecords);
            Assert.AreEqual(stateFromDb.LastUpdated, message.Timestamp);

            A.CallTo(() => _dao.Save(stateFromDb)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _dispatcher.Dispatch(A<TlsRecordEvaluationsChanged>.That.Matches(a => a.Id == message.Id && a.TlsRecords == message.TlsRecords && a.CertificateResults == message.Certificates), snsTopicArn)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _dispatcher.Dispatch(A<CreateScheduledReminder>.That.Matches(a => a.ResourceId == hostName && a.Service == "Tls" && a.ScheduledTime.Second == 33), snsTopicArn)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task ShouldDeleteWhenDomainDeletedReceived()
        {
            string hostName = "testhostname";
            MxHostDeleted message = new MxHostDeleted(hostName);

            await _tlsEntity.Handle(message);

            A.CallTo(() => _dao.Delete(hostName)).MustHaveHappenedOnceExactly();
        }
    }
}
