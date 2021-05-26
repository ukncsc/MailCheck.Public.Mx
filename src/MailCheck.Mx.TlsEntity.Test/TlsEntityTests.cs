using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.XRay.Recorder.Core.Internal.Utils;
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
using MailCheck.Mx.TlsEntity.Entity.EmailSecurity;
using MailCheck.Mx.TlsEntity.Entity.Notifiers;
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
        private IEntityChangedPublisher _entityChangedPublisher;
        private IClock _clock;
        private ILogger<TlsEntity.Entity.TlsEntity> _log;
        private IChangeNotifiersComposite _changeNotifierComposite;

        [SetUp]
        public void SetUp()
        {
            _dao = A.Fake<ITlsEntityDao>();
            _log = A.Fake<ILogger<TlsEntity.Entity.TlsEntity>>();
            _clock = A.Fake<IClock>();
            _tlsEntityConfig = A.Fake<ITlsEntityConfig>();
            _dispatcher = A.Fake<IMessageDispatcher>();
            _domainStatusPublisher = A.Fake<IDomainStatusPublisher>();
            _entityChangedPublisher = A.Fake<IEntityChangedPublisher>();
            _changeNotifierComposite = A.Fake<IChangeNotifiersComposite>();

            _tlsEntity = new TlsEntity.Entity.TlsEntity(_dao, _clock, _tlsEntityConfig, _dispatcher, _domainStatusPublisher,
                _entityChangedPublisher, _changeNotifierComposite, _log);
        }
        
        [Test]
        public async Task ShouldUpdateAndDispatchPollPendingWhenScheduledReminderFirstReceived()
        {
            const string hostName = "testhostname";
            const string snsTopicArn = "snsTopicArn";

            A.CallTo(() => _dao.Get(hostName)).Returns(new TlsEntityState(hostName) { TlsState = TlsState.Created, LastUpdated = null });
            A.CallTo(() => _tlsEntityConfig.SnsTopicArn).Returns(snsTopicArn);

            await _tlsEntity.Handle(new TlsScheduledReminder(Guid.NewGuid().ToString(), hostName));

            A.CallTo(() => _dao.Save(A<TlsEntityState>.That.Matches(e => e.TlsState == TlsState.PollPending))).MustHaveHappenedOnceExactly();
            A.CallTo(() => _dispatcher.Dispatch(A<TlsTestPending>.That.Matches(entity => entity.Id == hostName), snsTopicArn)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task ShouldUpdateAndDispatchPollPendingWhenScheduledReminderArrivesAfterTtl()
        {
            const string hostName = "testhostname";
            const string snsTopicArn = "snsTopicArn";
            DateTime testDate = new DateTime(2000, 01, 01);

            A.CallTo(() => _clock.GetDateTimeUtc()).Returns(testDate);
            A.CallTo(() => _dao.Get(hostName)).Returns(new TlsEntityState(hostName) { TlsState = TlsState.Created, LastUpdated = testDate - TimeSpan.FromSeconds(3) });
            A.CallTo(() => _tlsEntityConfig.TlsResultsCacheInSeconds).Returns(2);
            A.CallTo(() => _tlsEntityConfig.SnsTopicArn).Returns(snsTopicArn);

            await _tlsEntity.Handle(new TlsScheduledReminder(Guid.NewGuid().ToString(), hostName));
            A.CallTo(() => _dao.Save(A<TlsEntityState>.That.Matches(e => e.TlsState == TlsState.PollPending))).MustHaveHappenedOnceExactly();
            A.CallTo(() => _dispatcher.Dispatch(A<TlsTestPending>.That.Matches(entity => entity.Id == hostName), snsTopicArn)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task ShouldNotUpdateOrDispatchPollPendingWhenScheduledReminderArrivesBeforeTtl()
        {
            const string hostName = "testhostname";
            DateTime testDate = new DateTime(2000, 01, 01);

            A.CallTo(() => _clock.GetDateTimeUtc()).Returns(testDate);
            A.CallTo(() => _dao.Get(hostName)).Returns(new TlsEntityState(hostName) { TlsState = TlsState.Created, LastUpdated = testDate - TimeSpan.FromSeconds(1) });
            A.CallTo(() => _tlsEntityConfig.TlsResultsCacheInSeconds).Returns(2);

            await _tlsEntity.Handle(new TlsScheduledReminder(Guid.NewGuid().ToString(), hostName));

            A.CallTo(() => _dao.Save(A<TlsEntityState>._)).MustNotHaveHappened();
            A.CallTo(() => _dispatcher.Dispatch(A<TlsTestPending>._, A<string>._)).MustNotHaveHappened();
        }

        [Test]
        public async Task ShouldUpdateAndDispatchPollPendingWhenMxHostTestPendingFirstReceived()
        {
            const string hostName = "testhostname";
            const string snsTopicArn = "snsTopicArn";

            A.CallTo(() => _dao.Get(hostName)).Returns(new TlsEntityState(hostName) { TlsState = TlsState.Created, LastUpdated = null });
            A.CallTo(() => _tlsEntityConfig.SnsTopicArn).Returns(snsTopicArn);

            await _tlsEntity.Handle(new TlsScheduledReminder(Guid.NewGuid().ToString(), hostName));

            A.CallTo(() => _dao.Save(A<TlsEntityState>.That.Matches(e => e.TlsState == TlsState.PollPending))).MustHaveHappenedOnceExactly();
            A.CallTo(() => _dispatcher.Dispatch(A<TlsTestPending>.That.Matches(entity => entity.Id == hostName), snsTopicArn)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task ShouldUpdateAndDispatchPollPendingWhenMxHostTestPendingArrivesAfterTtl()
        {
            const string hostName = "testhostname";
            const string snsTopicArn = "snsTopicArn";
            DateTime testDate = new DateTime(2000, 01, 01);

            A.CallTo(() => _clock.GetDateTimeUtc()).Returns(testDate);
            A.CallTo(() => _dao.Get(hostName)).Returns(new TlsEntityState(hostName) { TlsState = TlsState.Created, LastUpdated = testDate - TimeSpan.FromSeconds(3) });
            A.CallTo(() => _tlsEntityConfig.TlsResultsCacheInSeconds).Returns(2);
            A.CallTo(() => _tlsEntityConfig.SnsTopicArn).Returns(snsTopicArn);

            await _tlsEntity.Handle(new MxHostTestPending(hostName));
            A.CallTo(() => _dao.Save(A<TlsEntityState>.That.Matches(e => e.TlsState == TlsState.PollPending))).MustHaveHappenedOnceExactly();
            A.CallTo(() => _dispatcher.Dispatch(A<TlsTestPending>.That.Matches(entity => entity.Id == hostName), snsTopicArn)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task ShouldNotUpdateOrDispatchPollPendingWhenMxHostTestPendingArrivesBeforeTtl()
        {
            const string hostName = "testhostname";
            DateTime testDate = new DateTime(2000, 01, 01);

            A.CallTo(() => _clock.GetDateTimeUtc()).Returns(testDate);
            A.CallTo(() => _dao.Get(hostName)).Returns(new TlsEntityState(hostName) { TlsState = TlsState.Created, LastUpdated = testDate - TimeSpan.FromSeconds(1) });
            A.CallTo(() => _tlsEntityConfig.TlsResultsCacheInSeconds).Returns(2);

            await _tlsEntity.Handle(new TlsScheduledReminder(Guid.NewGuid().ToString(), hostName));

            A.CallTo(() => _dao.Save(A<TlsEntityState>._)).MustNotHaveHappened();
            A.CallTo(() => _dispatcher.Dispatch(A<TlsTestPending>._, A<string>._)).MustNotHaveHappened();
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

            A.CallTo(() => _changeNotifierComposite.Handle(stateFromDb, message, A<List<string>>._)).MustNotHaveHappened();
            A.CallTo(() => _dao.Save(stateFromDb)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _dispatcher.Dispatch(A<TlsRecordEvaluationsChanged>.That.Matches(a => a.Id == message.Id && a.TlsRecords == message.TlsRecords && a.CertificateResults == message.Certificates), snsTopicArn, null)).MustNotHaveHappened();
            A.CallTo(() => _dispatcher.Dispatch(A<CreateScheduledReminder>.That.Matches(a => a.ResourceId == hostName && a.Service == "Tls"), snsTopicArn)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task ShouldHandleChangeSaveAndDispatchWhenTlsTestResultIsReceived()
        {
            string snsTopicArn = "snsTopicArn";
            A.CallTo(() => _tlsEntityConfig.SnsTopicArn).Returns(snsTopicArn);

            string hostName = "testhostname";

            TlsEntityState stateFromDb = new TlsEntityState(hostName) { TlsState = TlsState.Created };
            A.CallTo(() => _dao.Get(hostName)).Returns(Task.FromResult(stateFromDb));
            A.CallTo(() => _dao.GetDomainsFromHost(hostName)).Returns(Task.FromResult(new List<string>{ "test.gov.uk" }));



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
            List<string> domains = new List<string>();
            A.CallTo(() => _changeNotifierComposite.Handle(stateFromDb, message, A<List<string>>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _entityChangedPublisher.Publish(A<string>._, A<TlsEntityState>._,"TlsResultsEvaluated")).MustHaveHappenedOnceExactly();
            A.CallTo(() => _dao.Save(stateFromDb)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _dispatcher.Dispatch(A<TlsRecordEvaluationsChanged>.That.Matches(a => a.Id == message.Id && a.TlsRecords == message.TlsRecords && a.CertificateResults == message.Certificates), snsTopicArn)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _dispatcher.Dispatch(A<CreateScheduledReminder>.That.Matches(a => a.ResourceId == hostName && a.Service == "Tls"), snsTopicArn)).MustHaveHappenedOnceExactly();
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
