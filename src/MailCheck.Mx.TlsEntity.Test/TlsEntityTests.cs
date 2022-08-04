using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using MailCheck.Common.Contracts.Messaging;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Common.Util;
using MailCheck.Mx.Contracts.Entity;
using MailCheck.Mx.Contracts.External;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Simplified;
using MailCheck.Mx.Contracts.TlsEntity;
using MailCheck.Mx.Contracts.TlsEvaluator;
using MailCheck.Mx.TlsEntity.Config;
using MailCheck.Mx.TlsEntity.Dao;
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
        private ISimplifiedTlsEntityDao _hostnameIpaddressDao;

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
            _hostnameIpaddressDao = A.Fake<ISimplifiedTlsEntityDao>();

            A.CallTo(() => _tlsEntityConfig.SnsTopicArn).Returns("testSnsTopicArn");

            _tlsEntity = new TlsEntity.Entity.TlsEntity(_dao, _clock, _tlsEntityConfig, _dispatcher, _domainStatusPublisher, _entityChangedPublisher, _changeNotifierComposite, _log, _hostnameIpaddressDao);
        }

        [Test]
        public async Task ShouldNotUpdateAndDispatchPollPendingWhenMxHostTestPendingArrivesAfterTtl()
        {
            const string hostName = "testhostname";
            const string snsTopicArn = "snsTopicArn";
            DateTime testDate = new DateTime(2000, 01, 01);

            A.CallTo(() => _clock.GetDateTimeUtc()).Returns(testDate);
            A.CallTo(() => _dao.Get(hostName)).Returns(new TlsEntityState(hostName) { TlsState = TlsState.Created, LastUpdated = testDate - TimeSpan.FromSeconds(3) });
            A.CallTo(() => _tlsEntityConfig.TlsResultsCacheInSeconds).Returns(2);
            A.CallTo(() => _tlsEntityConfig.SnsTopicArn).Returns(snsTopicArn);

            await _tlsEntity.Handle(new MxHostTestPending(hostName,  new List<string>()));
            A.CallTo(() => _dao.Save(A<TlsEntityState>._)).MustNotHaveHappened();
            A.CallTo(() => _dispatcher.Dispatch(A<TlsTestPending>._, snsTopicArn)).MustNotHaveHappened();
        }

        [Test]
        public async Task ShouldDispatchCreateScheduledReminderWhenMxHostTestPendingArrives()
        {
            MxHostTestPending message = new MxHostTestPending("testHostname", new List<string>());

            await _tlsEntity.Handle(message);

            A.CallTo(() => _dispatcher.Dispatch(A<CreateScheduledReminder>.That.Matches(_ => _.ResourceId == "testhostname" && _.Service == "Tls" && _.ScheduledTime != default), "testSnsTopicArn")).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task ShouldCallIpAddressSyncMethodWhenIpsArePresent()
        {
            string hostname = "testHostname".ToLower(); // it is lowered by the code
            string ipAddress1 = "127.0.0.1";
            string ipAddress2 = "127.0.0.2";
            string ipAddress3 = "127.0.0.3";
            List<string> ipList = new List<string>() {
                ipAddress1,
                ipAddress2,
                ipAddress3
            };
            MxHostTestPending message = new MxHostTestPending("testHostname", ipList);

            await _tlsEntity.Handle(message);

            A.CallTo(_hostnameIpaddressDao)
               .Where(call => call.Method.Name == "SyncIpAddressForHostname"
                   && call.GetArgument<string>(0) == hostname
                   && Enumerable.SequenceEqual(call.GetArgument<List<string>>(1), ipList)
               ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task ShouldCallIpAddressSyncMethodWhenIpsAreNotPresent()
        {
            // This test covers the situation where no IPs are present.
            // But this might indicate that IPs need to be deleted so we will still call SyncIpAddressForHostname
            string hostname = "testHostname".ToLower(); // it is lowered by the code
            List<string> ipList = new List<string>();

            MxHostTestPending message = new MxHostTestPending(hostname, ipList);

            await _tlsEntity.Handle(message);
            A.CallTo(_hostnameIpaddressDao)
                .Where(call => call.Method.Name == "SyncIpAddressForHostname"
                    && call.GetArgument<string>(0) == hostname
                    && Enumerable.SequenceEqual(call.GetArgument<List<string>>(1), ipList)
                ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task ShouldAttemptToDispatchScheduledReminderForNewIps()
        {
            MxHostTestPending message = new MxHostTestPending("testHostname", new List<string> { "127.0.0.1", "127.0.0.2", "127.0.0.3" });

            await _tlsEntity.Handle(message);

            A.CallTo(() => _dispatcher.Dispatch(A<CreateScheduledReminder>.That.Matches(_ => _.ResourceId == "127.0.0.1" && _.Service == "SimplifiedTls" && _.ScheduledTime != default), "testSnsTopicArn")).MustHaveHappenedOnceExactly();
            A.CallTo(() => _dispatcher.Dispatch(A<CreateScheduledReminder>.That.Matches(_ => _.ResourceId == "127.0.0.2" && _.Service == "SimplifiedTls" && _.ScheduledTime != default), "testSnsTopicArn")).MustHaveHappenedOnceExactly();
            A.CallTo(() => _dispatcher.Dispatch(A<CreateScheduledReminder>.That.Matches(_ => _.ResourceId == "127.0.0.3" && _.Service == "SimplifiedTls" && _.ScheduledTime != default), "testSnsTopicArn")).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task ShouldNotDispatchCreateScheduledReminderWhenTlsResultsEvaluatedArrivesForNewHost()
        {
            TlsResultsEvaluated message = new TlsResultsEvaluated("testHostname", false, null);

            A.CallTo(() => _dao.Get("testhostname")).Returns(Task.FromResult((TlsEntityState)null));
            await _tlsEntity.Handle(message);

            A.CallTo(() => _dispatcher.Dispatch(A<CreateScheduledReminder>.That.Matches(_ => _.ResourceId == "testhostname" && _.Service == "Tls" && _.ScheduledTime == new DateTime()), "testSnsTopicArn")).MustNotHaveHappened();
        }

        [Test]
        public async Task ShouldNotTriggerTestWhenTlsResultsEvaluatedArrivesForNewHost()
        {
            TlsResultsEvaluated message = new TlsResultsEvaluated("testHostname", false, null);

            A.CallTo(() => _dao.Get("testhostname")).Returns(Task.FromResult((TlsEntityState)null));
            await _tlsEntity.Handle(message);

            A.CallTo(() => _dispatcher.Dispatch(A<TlsTestPending>.That.Matches(_ => _.Id == "testhostname"), "testSnsTopicArn")).MustNotHaveHappened();
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
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS))
            ), certificateResults);

            await _tlsEntity.Handle(message);

            A.CallTo(() => _entityChangedPublisher.Publish(A<string>._, A<TlsEntityState>._,"TlsResultsEvaluated")).MustNotHaveHappened();
            A.CallTo(() => _changeNotifierComposite.Handle(stateFromDb, message, A<List<string>>._)).MustNotHaveHappened();
            A.CallTo(() => _dao.Save(stateFromDb)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _dispatcher.Dispatch(A<TlsRecordEvaluationsChanged>.That.Matches(a => a.Id == message.Id && a.TlsRecords == message.TlsRecords && a.CertificateResults == message.Certificates), snsTopicArn, null)).MustNotHaveHappened();
            A.CallTo(() => _dispatcher.Dispatch(A<ReminderSuccessful>.That.Matches(a => a.ResourceId == hostName && a.Service == "Tls"), snsTopicArn)).MustHaveHappenedOnceExactly();
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
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS))
            ), certificateResults);

            await _tlsEntity.Handle(message);

            Assert.AreEqual(stateFromDb.TlsState, TlsState.Evaluated);
            Assert.AreEqual(stateFromDb.TlsRecords, message.TlsRecords);
            Assert.AreEqual(stateFromDb.LastUpdated, message.Timestamp);
            List<string> domains = new List<string>();
            A.CallTo(() => _changeNotifierComposite.Handle(stateFromDb, message, A<List<string>>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _dao.Save(stateFromDb)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _dispatcher.Dispatch(A<ReminderSuccessful>.That.Matches(a => a.ResourceId == hostName && a.Service == "Tls"), snsTopicArn)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task ShouldTreatTlsResultsEvaluatedTlsRecordsAsNullAndCertErrorsAsEmpty()
        {
            string snsTopicArn = "snsTopicArn";
            A.CallTo(() => _tlsEntityConfig.SnsTopicArn).Returns(snsTopicArn);

            string hostName = "testhostname";

            TlsEntityState stateFromDb = new TlsEntityState(hostName) { TlsState = TlsState.Created };
            A.CallTo(() => _dao.Get(hostName)).Returns(Task.FromResult(stateFromDb));
            A.CallTo(() => _dao.GetDomainsFromHost(hostName)).Returns(Task.FromResult(new List<string>{ "test.gov.uk" }));

            A.CallTo(() => _tlsEntityConfig.NextScheduledInSeconds).Returns(33);
            A.CallTo(() => _clock.GetDateTimeUtc()).Returns(DateTime.MinValue);

            CertificateResults certificateResults = new CertificateResults(null, new List<Error> { new Error(ErrorType.Error, "testError", "testMD") });

            TlsResultsEvaluated message = new TlsResultsEvaluated(hostName, false, new TlsRecords(
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.WARNING)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.WARNING)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.WARNING)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.WARNING)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.WARNING)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.WARNING)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.WARNING)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.WARNING)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.WARNING)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.WARNING)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.WARNING)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.WARNING)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.WARNING)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.WARNING)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.WARNING)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.WARNING)),
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.WARNING))
            ), certificateResults);

            await _tlsEntity.Handle(message);

            List<string> domains = new List<string>();
            A.CallTo(() => _changeNotifierComposite.Handle(stateFromDb, A<TlsResultsEvaluated>.That.Matches(
                x => x.TlsRecords.Records.Count == 0 && x.Certificates.Errors.Count == 0), A<List<string>>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _dao.Save(A<TlsEntityState>.That.Matches(
                x => x.TlsRecords.Records.Count == 0 && x.CertificateResults.Errors.Count == 0))).MustHaveHappenedOnceExactly();
            A.CallTo(() => _dispatcher.Dispatch(A<ReminderSuccessful>.That.Matches(a => a.ResourceId == hostName && a.Service == "Tls"), snsTopicArn)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task ShouldDeleteWhenDomainDeletedReceived()
        {
            string hostName = "testhostname";
            MxHostDeleted message = new MxHostDeleted(hostName);

            await _tlsEntity.Handle(message);

            A.CallTo(() => _dao.Delete(hostName)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _dispatcher.Dispatch(A<DeleteScheduledReminder>.That.Matches(_ =>
                _.ResourceId == hostName && _.Service == "Tls"), A<string>._)).MustHaveHappenedOnceExactly();
        }
    }
}
