using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FakeItEasy;
using MailCheck.Common.Contracts.Messaging;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Common.Messaging.Common.Exception;
using MailCheck.Common.Util;
using MailCheck.Mx.Contracts.Entity;
using MailCheck.Mx.Contracts.External;
using MailCheck.Mx.Contracts.Poller;
using MailCheck.Mx.Entity.Config;
using MailCheck.Mx.Entity.Dao;
using MailCheck.Mx.Entity.Entity;
using MailCheck.Mx.Entity.Entity.Notifiers;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace MailCheck.Mx.Entity.Test
{
    [TestFixture]
    public class MxEntityTests
    {
        private MxEntity _mxEntity;
        private IMxEntityDao _dao;
        private IMxEntityConfig _mxEntityConfig;
        private IMessageDispatcher _dispatcher;
        private IChangeNotifiersComposite _changeNotifiersComposite;
        private IClock _clock;
        private ILogger<MxEntity> _log;

        [SetUp]
        public void SetUp()
        {
            _dao = A.Fake<IMxEntityDao>();
            _log = A.Fake<ILogger<MxEntity>>();
            _clock = A.Fake<IClock>();
            _mxEntityConfig = A.Fake<IMxEntityConfig>();
            _dispatcher = A.Fake<IMessageDispatcher>();
            _changeNotifiersComposite = A.Fake<IChangeNotifiersComposite>();

            _mxEntity = new MxEntity(_dao, _mxEntityConfig, _dispatcher, _changeNotifiersComposite, _clock, _log);
        }

        [Test]
        public async Task ShouldThrowWhenDomainCreatedReceivedAndDomainAlreadyExists()
        {
            string domainName = "testDomainName";

            A.CallTo(() => _dao.Get(domainName)).Returns(Task.FromResult(new MxEntityState("")));

            await _mxEntity.Handle(new DomainCreated(domainName, string.Empty, DateTime.MaxValue));

            A.CallTo(() => _dao.Save(A<MxEntityState>.Ignored)).MustNotHaveHappened();
        }

        [Test]
        public async Task ShouldSaveAndDispatchMxEntityCreatedWhenDomainCreatedReceivedAndDomainDoesNotExists()
        {
            string domainName = "testDomainName";
            string snsTopicArn = "SnsTopicArn";

            A.CallTo(() => _dao.Get(domainName.ToLower())).Returns(Task.FromResult((MxEntityState)null));
            A.CallTo(() => _mxEntityConfig.SnsTopicArn).Returns(snsTopicArn);
            A.CallTo(() => _clock.GetDateTimeUtc()).Returns(DateTime.MinValue);

            await _mxEntity.Handle(new DomainCreated(domainName, string.Empty, DateTime.MaxValue));

            A.CallTo(() => _dao.Save(A<MxEntityState>.That.Matches(state => state.MxState == MxState.Created))).MustHaveHappenedOnceExactly();
            A.CallTo(() => _dispatcher.Dispatch(A<MxEntityCreated>.That.Matches(entity => entity.Id == domainName.ToLower()), snsTopicArn)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _dispatcher.Dispatch(A<CreateScheduledReminder>.That.Matches(a => a.ResourceId == domainName.ToLower() && a.Service == "Mx" && a.ScheduledTime == DateTime.MinValue), snsTopicArn)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task ShouldDispatchPollPendingWhenScheduledReminderReceived()
        {
            string domainName = "testDomainName";
            string snsTopicArn = "SnsTopicArn";
            MxEntityState stateFromDb = new MxEntityState(domainName.ToLower()) { MxState = MxState.Created };

            string expectedDomainName = "testdomainname";

            A.CallTo(() => _dao.Get(expectedDomainName)).Returns(stateFromDb);
            A.CallTo(() => _mxEntityConfig.SnsTopicArn).Returns(snsTopicArn);

            await _mxEntity.Handle(new MxScheduledReminder(Guid.NewGuid().ToString(), domainName));

            A.CallTo(() => _dao.UpdateState(expectedDomainName, MxState.PollPending)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _dispatcher.Dispatch(A<MxPollPending>.That.Matches(entity => entity.Id == domainName.ToLower()),
              snsTopicArn)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void ShouldThrowWhenMxRecordsPolledReceivedButDomainDoesNotExist()
        {
            A.CallTo(() => _dao.Get(A<string>._)).Returns(Task.FromResult((MxEntityState)null));

            Assert.ThrowsAsync<MailCheckException>(async () =>
            {
                await _mxEntity.Handle(new MxRecordsPolled("", new List<HostMxRecord>(), null));
            });
        }

        [Test]
        public async Task ShouldHandleChangeSaveAndDispatchWhenMxRecordsPolledReceived()
        {
            string snsTopicArn = "SnsTopicArn";
            A.CallTo(() => _mxEntityConfig.SnsTopicArn).Returns(snsTopicArn);

            string domainName = "testDomainName";
            MxEntityState stateFromDb = new MxEntityState(domainName.ToLower()) { MxState = MxState.Created };
            A.CallTo(() => _dao.Get(domainName.ToLower())).Returns(Task.FromResult(stateFromDb));


            A.CallTo(() => _mxEntityConfig.NextScheduledInSeconds).Returns(33);
            A.CallTo(() => _clock.GetDateTimeUtc()).Returns(DateTime.MinValue);

            MxRecordsPolled message = new MxRecordsPolled(domainName, new List<HostMxRecord>(), null) { Timestamp = DateTime.UnixEpoch };

            await _mxEntity.Handle(message);

            Assert.AreEqual(stateFromDb.MxState, MxState.Evaluated);
            Assert.AreEqual(stateFromDb.HostMxRecords, message.Records);
            Assert.AreEqual(stateFromDb.LastUpdated, message.Timestamp);

            A.CallTo(() => _changeNotifiersComposite.Handle(stateFromDb, message)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _dao.Save(stateFromDb)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _dispatcher.Dispatch(A<MxRecordsUpdated>.That.Matches(a => a.Id == message.Id.ToLower() && a.Records.Count == 0), snsTopicArn)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _dispatcher.Dispatch(A<CreateScheduledReminder>.That.Matches(a => a.ResourceId == domainName.ToLower() && a.Service == "Mx" && a.ScheduledTime.Second == 33), snsTopicArn)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task ShouldDeleteWhenDomainDeletedReceived()
        {
            string domainName = "testDomainName";
            string hostName = "testHostName";

            DomainDeleted message = new DomainDeleted(domainName);

            MxEntityState stateFromDb = new MxEntityState(domainName.ToLower())
            {
                MxState = MxState.Created, HostMxRecords = new List<HostMxRecord>
                {
                    new HostMxRecord(hostName, 0, new List<string>())
                }
            };
            A.CallTo(() => _dao.Get(domainName.ToLower())).Returns(Task.FromResult(stateFromDb));
            await _mxEntity.Handle(message);

            A.CallTo(() => _dao.Delete(domainName.ToLower())).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task ShouldDeleteDomainAndHostWhenDomainDeletedReceivedAndHostIsNotStillInUse()
        {
            string domainName = "test.gov.uk";
            string hostName = "test-host-inbound.com";

            DomainDeleted message = new DomainDeleted(domainName);

            MxEntityState stateFromDb = new MxEntityState(domainName.ToLower())
            {
                MxState = MxState.Created, HostMxRecords = new List<HostMxRecord>
                {
                    new HostMxRecord(hostName, 0, new List<string>())
                }
            };

            List<string> uniqueHosts = new List<string>{hostName};

            A.CallTo(() => _dao.Get(domainName.ToLower())).Returns(Task.FromResult(stateFromDb));
            A.CallTo(() => _dao.GetHostsUniqueToDomain(domainName.ToLower())).Returns(Task.FromResult(uniqueHosts));

            await _mxEntity.Handle(message);

            A.CallTo(() => _dao.DeleteHosts(uniqueHosts)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _dispatcher.Dispatch(A<MxHostDeleted>.That.Matches(a => a.Id == stateFromDb.HostMxRecords[0].Id), A<string>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _dao.Delete(domainName.ToLower())).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task ShouldDeleteDomainOnlyWhenDomainDeletedReceivedAndHostIsStillInUse()
        {
            string domainName = "test.gov.uk";
            string hostName = "test-host-inbound.com";

            DomainDeleted message = new DomainDeleted(domainName);

            MxEntityState stateFromDb = new MxEntityState(domainName.ToLower())
            {
                MxState = MxState.Created, HostMxRecords = new List<HostMxRecord>
                {
                    new HostMxRecord(hostName, 0, new List<string>())
                }
            };

            List<string> uniqueHosts = new List<string>();

            A.CallTo(() => _dao.Get(domainName.ToLower())).Returns(Task.FromResult(stateFromDb));
            A.CallTo(() => _dao.GetHostsUniqueToDomain(domainName.ToLower())).Returns(Task.FromResult(uniqueHosts));

            await _mxEntity.Handle(message);

            A.CallTo(() => _dao.DeleteHosts(uniqueHosts)).MustNotHaveHappened();
            A.CallTo(() => _dispatcher.Dispatch(A<MxHostDeleted>.That.Matches(a => a.Id == stateFromDb.HostMxRecords[0].Id), A<string>._)).MustNotHaveHappened();
            A.CallTo(() => _dao.Delete(domainName.ToLower())).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task ShouldDispatchMultipleMxHostDeletedMessagesWhenDomainDeletedReceivedAndMultipleHosts()
        {
            string domainName = "test.gov.uk";
            string hostName1 = "test-host-inbound1.com";
            string hostName2 = "test-host-inbound2.com";


            DomainDeleted message = new DomainDeleted(domainName);

            MxEntityState stateFromDb = new MxEntityState(domainName.ToLower())
            {
                MxState = MxState.Created, HostMxRecords = new List<HostMxRecord>
                {
                    new HostMxRecord(hostName1, 0, new List<string>()),
                    new HostMxRecord(hostName2, 0, new List<string>())
                }
            };

            List<string> uniqueHosts = new List<string>{hostName1, hostName2};

            A.CallTo(() => _dao.Get(domainName.ToLower())).Returns(Task.FromResult(stateFromDb));
            A.CallTo(() => _dao.GetHostsUniqueToDomain(domainName.ToLower())).Returns(Task.FromResult(uniqueHosts));

            await _mxEntity.Handle(message);

            A.CallTo(() => _dao.DeleteHosts(uniqueHosts)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _dispatcher.Dispatch(A<MxHostDeleted>.That.Matches(a => a.Id == stateFromDb.HostMxRecords[0].Id), A<string>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _dispatcher.Dispatch(A<MxHostDeleted>.That.Matches(a => a.Id == stateFromDb.HostMxRecords[1].Id), A<string>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _dao.Delete(domainName.ToLower())).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task ShouldOmitErroneousHostsFound()
        {
            string domainName = "test.gov.uk";
            string hostName1 = "test-host-inbound1 .com";
            string hostName2 = "test-host-inbound2.com";


            string snsTopicArn = "SnsTopicArn";
            A.CallTo(() => _mxEntityConfig.SnsTopicArn).Returns(snsTopicArn);

            List<HostMxRecord> hostMxRecords = new List<HostMxRecord>
            {
                new HostMxRecord(hostName1, 0, new List<string>()),
                new HostMxRecord(hostName2, 0, new List<string>()),
            };

            MxEntityState stateFromDb = new MxEntityState(domainName.ToLower())
            {
                MxState = MxState.Created,
                HostMxRecords = hostMxRecords,
            };

            A.CallTo(() => _dao.Get(domainName.ToLower())).Returns(Task.FromResult(stateFromDb));


            A.CallTo(() => _mxEntityConfig.NextScheduledInSeconds).Returns(33);
            A.CallTo(() => _clock.GetDateTimeUtc()).Returns(DateTime.MinValue);

            MxRecordsPolled message = new MxRecordsPolled(domainName, hostMxRecords, null) { Timestamp = DateTime.UnixEpoch };
            List<HostMxRecord> validRecords = new List<HostMxRecord> { new HostMxRecord(hostName2, 0, new List<string>())};
            await _mxEntity.Handle(message);

            Assert.AreEqual(stateFromDb.MxState, MxState.Evaluated);
            Assert.AreEqual(stateFromDb.HostMxRecords[0], message.Records[1]); // Make sure erroneous host is ommitted
            Assert.AreEqual(stateFromDb.LastUpdated, message.Timestamp);

            A.CallTo(() => _changeNotifiersComposite.Handle(stateFromDb, message)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _dao.Save(stateFromDb)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _dispatcher.Dispatch(A<MxHostTestPending>.That.Matches(a => a.Id == hostName1.ToLower()), snsTopicArn)).MustNotHaveHappened();
            A.CallTo(() => _dispatcher.Dispatch(A<MxHostTestPending>.That.Matches(a => a.Id == hostName2.ToLower()), snsTopicArn)).MustHaveHappenedOnceExactly();

        }
    }
}
