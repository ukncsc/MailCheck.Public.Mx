using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FakeItEasy;
using MailCheck.Common.Contracts.Findings;
using MailCheck.Common.Contracts.Messaging;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Common.Messaging.Common.Exception;
using MailCheck.Common.Util;
using MailCheck.Mx.Contracts.Entity;
using MailCheck.Mx.Contracts.External;
using MailCheck.Mx.Contracts.Poller;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Simplified;
using MailCheck.Mx.Entity.Config;
using MailCheck.Mx.Entity.Dao;
using MailCheck.Mx.Entity.Entity.Notifiers;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Message = MailCheck.Common.Messaging.Abstractions.Message;
using MessageType = MailCheck.Common.Contracts.Advisories.MessageType;

namespace MailCheck.Mx.Entity.Entity
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
        private IFindingFactory _findingFactory;
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
            _findingFactory = A.Fake<IFindingFactory>();

            _mxEntity = new MxEntity(_dao, _mxEntityConfig, _dispatcher, _changeNotifiersComposite, _clock, _findingFactory, _log);
        }

        [Test]
        public async Task ShouldThrowWhenDomainCreatedReceivedAndDomainAlreadyExists()
        {
            string domainName = "testDomainName";
            string snsTopicArn = "SnsTopicArn";

            A.CallTo(() => _dao.Get(domainName)).Returns(Task.FromResult(new MxEntityState("")));

            await _mxEntity.Handle(new DomainCreated(domainName, string.Empty, DateTime.MaxValue));

            A.CallTo(() => _dao.Save(A<MxEntityState>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => _dispatcher.Dispatch(A<EntityChanged>._, snsTopicArn)).MustNotHaveHappened();
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
            A.CallTo(() => _dispatcher.Dispatch(A<CreateScheduledReminder>.That.Matches(a => a.ResourceId == domainName.ToLower() && a.Service == "Mx" && a.ScheduledTime == default), snsTopicArn)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _dispatcher.Dispatch(A<EntityChanged>._, snsTopicArn)).MustHaveHappenedOnceExactly();
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
        public async Task ShouldDeleteWhenDomainDeletedReceived()
        {
            string domainName = "testDomainName";
            string hostName = "testHostName";

            DomainDeleted message = new DomainDeleted(domainName);

            MxEntityState stateFromDb = new MxEntityState(domainName.ToLower())
            {
                MxState = MxState.Created,
                HostMxRecords = new List<HostMxRecord>
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
                MxState = MxState.Created,
                HostMxRecords = new List<HostMxRecord>
                {
                    new HostMxRecord(hostName, 0, new List<string>())
                }
            };

            List<string> uniqueHosts = new List<string> { hostName };

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
                MxState = MxState.Created,
                HostMxRecords = new List<HostMxRecord>
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
                MxState = MxState.Created,
                HostMxRecords = new List<HostMxRecord>
                {
                    new HostMxRecord(hostName1, 0, new List<string>()),
                    new HostMxRecord(hostName2, 0, new List<string>())
                }
            };

            List<string> uniqueHosts = new List<string> { hostName1, hostName2 };

            A.CallTo(() => _dao.Get(domainName.ToLower())).Returns(Task.FromResult(stateFromDb));
            A.CallTo(() => _dao.GetHostsUniqueToDomain(domainName.ToLower())).Returns(Task.FromResult(uniqueHosts));

            await _mxEntity.Handle(message);

            A.CallTo(() => _dao.DeleteHosts(uniqueHosts)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _dispatcher.Dispatch(A<MxHostDeleted>.That.Matches(a => a.Id == stateFromDb.HostMxRecords[0].Id), A<string>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _dispatcher.Dispatch(A<MxHostDeleted>.That.Matches(a => a.Id == stateFromDb.HostMxRecords[1].Id), A<string>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _dao.Delete(domainName.ToLower())).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task ShouldDispatchDeleteScheduledReminderWhenDomainDeletedReceived()
        {
            string domainName = "testDomainName";
            string hostName = "testHostName";

            DomainDeleted message = new DomainDeleted(domainName);

            MxEntityState stateFromDb = new MxEntityState(domainName.ToLower())
            {
                MxState = MxState.Created,
                HostMxRecords = new List<HostMxRecord>
                {
                    new HostMxRecord(hostName, 0, new List<string>())
                }
            };
            A.CallTo(() => _dao.Get(domainName.ToLower())).Returns(Task.FromResult(stateFromDb));
            await _mxEntity.Handle(message);

            A.CallTo(() => _dispatcher.Dispatch(A<DeleteScheduledReminder>.That.Matches(_ =>
                _.ResourceId == domainName.ToLower() && _.Service == "Mx"), A<string>._)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task ShouldHandleChangeSaveAndDispatchWhenMxRecordsPolledReceived()
        {
            string snsTopicArn = "SnsTopicArn";
            A.CallTo(() => _mxEntityConfig.SnsTopicArn).Returns(snsTopicArn);

            string domainName = "testDomainName";
            MxEntityState stateFromDb = new MxEntityState(domainName.ToLower()) { MxState = MxState.Created };
            A.CallTo(() => _dao.Get(domainName.ToLower())).Returns(Task.FromResult(stateFromDb));

            A.CallTo(() => _clock.GetDateTimeUtc()).Returns(DateTime.MinValue);

            MxRecordsPolled message = new MxRecordsPolled(domainName, new List<HostMxRecord>{new HostMxRecord("host.com",null,null)}, null) { Timestamp = DateTime.UnixEpoch };

            await _mxEntity.Handle(message);

            Assert.AreEqual(stateFromDb.MxState, MxState.Evaluated);
            Assert.AreSame(stateFromDb.HostMxRecords[0], message.Records[0]);
            Assert.AreEqual(stateFromDb.LastUpdated, message.Timestamp);

            A.CallTo(() => _changeNotifiersComposite.Handle(stateFromDb, message)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _dao.Save(stateFromDb)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _dispatcher.Dispatch(A<MxRecordsUpdated>.That.Matches(a => a.Id == message.Id.ToLower() && a.Records.Count == 1), snsTopicArn)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _dispatcher.Dispatch(A<ReminderSuccessful>.That.Matches(a =>
                a.ResourceId == domainName.ToLower() &&
                a.Service == "Mx"), snsTopicArn)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _dispatcher.Dispatch(A<EntityChanged>._, snsTopicArn)).MustHaveHappenedOnceExactly();
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
            List<HostMxRecord> validRecords = new List<HostMxRecord> { new HostMxRecord(hostName2, 0, new List<string>()) };
            await _mxEntity.Handle(message);

            Assert.AreEqual(stateFromDb.MxState, MxState.Evaluated);
            Assert.AreEqual(stateFromDb.HostMxRecords[0], message.Records[1]); // Make sure erroneous host is ommitted
            Assert.AreEqual(stateFromDb.LastUpdated, message.Timestamp);

            A.CallTo(() => _changeNotifiersComposite.Handle(stateFromDb, message)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _dao.Save(stateFromDb)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _dispatcher.Dispatch(A<MxHostTestPending>.That.Matches(a => a.Id == hostName1.ToLower()), snsTopicArn)).MustNotHaveHappened();
            A.CallTo(() => _dispatcher.Dispatch(A<MxHostTestPending>.That.Matches(a => a.Id == hostName2.ToLower()), snsTopicArn)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task ShouldDispatchReminderForIps()
        {
            string domainName = "test.gov.uk";
            string hostName = "test-host-inbound2.com";

            string snsTopicArn = "SnsTopicArn";
            A.CallTo(() => _mxEntityConfig.SnsTopicArn).Returns(snsTopicArn);
            string ipAddress1 = "ipaddress1";
            string ipAddress2 = "ipaddress2";
            List<string> ipAddresses = new List<string>() { ipAddress1, ipAddress2 };
            List<HostMxRecord> hostMxRecords = new List<HostMxRecord>
            {
                new HostMxRecord(hostName, 0, ipAddresses),
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

            await _mxEntity.Handle(message);

            A.CallTo(() => _changeNotifiersComposite.Handle(stateFromDb, message)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _dao.Save(stateFromDb)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _dispatcher.Dispatch(A<MxHostTestPending>.That.Matches(a => a.Id == hostName.ToLower() && a.IpAddresses == ipAddresses), snsTopicArn)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void ShouldNotThrowForANonExistentEntityWhenHandlingDomainDeleted()
        {
            var domainDeleted = new DomainDeleted("ncsc.gov.uk");

            A.CallTo(() => _dao.Get(A<string>._)).Returns(Task.FromResult<MxEntityState>(null));

            Assert.DoesNotThrowAsync(async () =>
            {
                await _mxEntity.Handle(domainDeleted);
            });

            A.CallTo(() => _dao.Delete(A<string>._)).MustNotHaveHappened();
            A.CallTo(() => _dao.DeleteHosts(A<List<string>>._)).MustNotHaveHappened();
            A.CallTo(() => _dispatcher.Dispatch(A<Message>._, A<string>._)).MustNotHaveHappened();
        }

        [Test]
        public void ShouldNotThrowForANonExistentEntityWhenHandlingMxScheduledReminder()
        {
            var domainReminder = new MxScheduledReminder("ncsc.gov.uk", "resource");

            A.CallTo(() => _dao.Get(A<string>._)).Returns(Task.FromResult<MxEntityState>(null));

            Assert.DoesNotThrowAsync(async () =>
            {
                await _mxEntity.Handle(domainReminder);
            });

            A.CallTo(() => _dao.UpdateState(A<string>._, A<MxState>._)).MustNotHaveHappened();
            A.CallTo(() => _dispatcher.Dispatch(A<Message>._, A<string>._)).MustNotHaveHappened();
        }

        [Test]
        public void ShouldNotThrowForANonExistentEntityWhenHandlingMxRecordsPolled()
        {
            var domainPolled = new MxRecordsPolled("ncsc.gov.uk", new List<HostMxRecord>(), null);

            A.CallTo(() => _dao.Get(A<string>._)).Returns(Task.FromResult<MxEntityState>(null));

            Assert.DoesNotThrowAsync(async () =>
            {
                await _mxEntity.Handle(domainPolled);
            });

            A.CallTo(() => _changeNotifiersComposite.Handle(A<MxEntityState>._, A<Message>._)).MustNotHaveHappened();
            A.CallTo(() => _dao.Save(A<MxEntityState>._)).MustNotHaveHappened();
            A.CallTo(() => _dispatcher.Dispatch(A<Message>._, A<string>._)).MustNotHaveHappened();
        }

        [Test]
        public async Task ShouldRemoveFindingsWhenHostsChange()
        {
            HostMxRecord hostMxRecord1 = new HostMxRecord("host1", 0, new List<string>());
            HostMxRecord hostMxRecord2 = new HostMxRecord("host2", 0, new List<string>());
            HostMxRecord hostMxRecord3 = new HostMxRecord("host3", 0, new List<string>());

            List<HostMxRecord> oldRecords = new List<HostMxRecord> { hostMxRecord1, hostMxRecord2 };
            List<HostMxRecord> newRecords = new List<HostMxRecord> { hostMxRecord2, hostMxRecord3 };

            MxEntityState existingState = new MxEntityState("ncsc.gov.uk") { HostMxRecords = oldRecords};
            A.CallTo(() => _dao.Get(A<string>._)).Returns(Task.FromResult(existingState));

            NamedAdvisory host1TlsAdvisory = new NamedAdvisory(Guid.Empty, "host1TlsAdvisory", MessageType.error, null, null);
            NamedAdvisory host1CertAdvisory = new NamedAdvisory(Guid.Empty, "host1CertAdvisory", MessageType.error, null, null);
            Finding host1TlsFinding = new Finding { Name = "host1TlsFinding" };
            Finding host1CertFinding = new Finding { Name = "host1CertFinding" };

            A.CallTo(() => _findingFactory.Create(A<NamedAdvisory>.That.IsSameAs(host1TlsAdvisory), "ncsc.gov.uk", "host1")).Returns(host1TlsFinding);
            A.CallTo(() => _findingFactory.Create(A<NamedAdvisory>.That.IsSameAs(host1CertAdvisory), "ncsc.gov.uk", "host1")).Returns(host1CertFinding);

            SimplifiedTlsEntityState host1TlsEntityState = new SimplifiedTlsEntityState
            {
                TlsAdvisories = new List<NamedAdvisory> { host1TlsAdvisory },
                CertAdvisories = new List<NamedAdvisory> { host1CertAdvisory }
            };

            A.CallTo(() => _dao.GetSimplifiedStates("host1"))
                .Returns(Task.FromResult(new List<SimplifiedTlsEntityState> {host1TlsEntityState}));

            MxRecordsPolled pollResult = new MxRecordsPolled("ncsc.gov.uk", newRecords, null);
            await _mxEntity.Handle(pollResult);

            A.CallTo(() => _dispatcher.Dispatch(A<FindingsChanged>.That.Matches(
                    x => x.Removed.Contains(host1TlsFinding) &&
                         x.Removed.Contains(host1CertFinding) &&
                         x.Domain == "ncsc.gov.uk"), A<string>._))
                .MustHaveHappened();
        }

        [Test]
        public async Task ShouldHandleEmptyAdvisoriesWhenRemovingFindings()
        {
            HostMxRecord hostMxRecord1 = new HostMxRecord("host1", 0, new List<string>());
            HostMxRecord hostMxRecord2 = new HostMxRecord("host2", 0, new List<string>());
            HostMxRecord hostMxRecord3 = new HostMxRecord("host3", 0, new List<string>());

            List<HostMxRecord> oldRecords = new List<HostMxRecord> { hostMxRecord1, hostMxRecord2 };
            List<HostMxRecord> newRecords = new List<HostMxRecord> { hostMxRecord2, hostMxRecord3 };

            MxEntityState existingState = new MxEntityState("ncsc.gov.uk") { HostMxRecords = oldRecords };
            A.CallTo(() => _dao.Get(A<string>._)).Returns(Task.FromResult(existingState));

            SimplifiedTlsEntityState host1TlsEntityState = new SimplifiedTlsEntityState
            {
                TlsAdvisories = null,
                CertAdvisories = null
            };

            A.CallTo(() => _dao.GetSimplifiedStates("host1"))
                .Returns(Task.FromResult(new List<SimplifiedTlsEntityState> { host1TlsEntityState }));

            MxRecordsPolled pollResult = new MxRecordsPolled("ncsc.gov.uk", newRecords, null);
            await _mxEntity.Handle(pollResult);

            A.CallTo(() => _dispatcher.Dispatch(A<FindingsChanged>._, A<string>._)).MustNotHaveHappened();
        }

        [Test]
        public async Task ShouldNotRemoveFindingsWhenHostsSame()
        {
            HostMxRecord hostMxRecord1 = new HostMxRecord("host1", 0, new List<string>());
            HostMxRecord hostMxRecord2 = new HostMxRecord("host2", 0, new List<string>());

            List<HostMxRecord> oldRecords = new List<HostMxRecord> { hostMxRecord1, hostMxRecord2 };
            List<HostMxRecord> newRecords = new List<HostMxRecord> { hostMxRecord1, hostMxRecord2 };

            MxEntityState existingState = new MxEntityState("ncsc.gov.uk") { HostMxRecords = oldRecords };
            A.CallTo(() => _dao.Get(A<string>._)).Returns(Task.FromResult(existingState));

            MxRecordsPolled pollResult = new MxRecordsPolled("ncsc.gov.uk", newRecords, null);
            await _mxEntity.Handle(pollResult);

            A.CallTo(() => _dispatcher.Dispatch(A<FindingsChanged>._, A<string>._)).MustNotHaveHappened();
        }
    }
}