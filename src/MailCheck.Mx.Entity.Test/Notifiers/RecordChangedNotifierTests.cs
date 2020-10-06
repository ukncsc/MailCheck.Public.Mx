using System;
using System.Collections.Generic;
using FakeItEasy;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Mx.Contracts.Entity;
using MailCheck.Mx.Contracts.Poller;
using MailCheck.Mx.Entity.Config;
using MailCheck.Mx.Entity.Entity.Notifications;
using MailCheck.Mx.Entity.Entity.Notifiers;
using NUnit.Framework;

namespace MailCheck.Mx.Entity.Test.Notifiers
{
    [TestFixture]
    public class RecordChangedNotifierTests
    {
        private IMessageDispatcher _dispatcher;
        private IMxEntityConfig _mxEntityConfig;
        private IEqualityComparer<Message> _hostMxEqualityComparer;
        private RecordChangedNotifier _recordChangedNotifier;

        [SetUp]
        public void SetUp()
        {
            _dispatcher = A.Fake<IMessageDispatcher>();
            _mxEntityConfig = A.Fake<IMxEntityConfig>();
            _hostMxEqualityComparer = new MessageEqualityComparer();
            _recordChangedNotifier = new RecordChangedNotifier(_dispatcher, _mxEntityConfig, _hostMxEqualityComparer);
        }

        [Test]
        public void DoesNotNotifyWhenNoChanges()
        {
            string testDomain = "domain";
            string testHostName = "hostname";

            MxEntityState state = new MxEntityState(testDomain);
            HostMxRecord record = new HostMxRecord(testHostName, 5, new List<string> {"192.168.0.1"});
            state.HostMxRecords = new List<HostMxRecord> {record};
            List<HostMxRecord> hostMxRecords = new List<HostMxRecord> {record};
            MxRecordsPolled mxRecordsPolled = new MxRecordsPolled(testDomain, hostMxRecords, TimeSpan.MinValue);

            _recordChangedNotifier.Handle(state, mxRecordsPolled);

            A.CallTo(() => _dispatcher.Dispatch(A<Message>._, A<string>._, A<string>._)).MustNotHaveHappened();
        }

        [Test]
        public void NotifiesWhenMxRecordAdded()
        {
            string testDomain = "domain";
            string testHostName = "hostname";

            MxEntityState state = new MxEntityState(testDomain);
            HostMxRecord record = new HostMxRecord(testHostName, 5, new List<string> {"192.168.0.1"});
            state.HostMxRecords = new List<HostMxRecord>();
            List<HostMxRecord> hostMxRecords = new List<HostMxRecord> {record};
            MxRecordsPolled mxRecordsPolled = new MxRecordsPolled(testDomain, hostMxRecords, TimeSpan.MinValue);

            _recordChangedNotifier.Handle(state, mxRecordsPolled);

            A.CallTo(() => _dispatcher.Dispatch(A<MxRecordAdded>._, A<string>._))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() =>
                    _dispatcher.Dispatch(A<MxRecordAdded>.That.Matches(x => x.Id == testDomain), A<string>._))
                .MustHaveHappenedOnceExactly();
        }


        [Test]
        public void NotifiesWhenRecordRemoved()
        {
            string testDomain = "domain";
            string testHostName = "hostname";

            MxEntityState state = new MxEntityState(testDomain);
            HostMxRecord record = new HostMxRecord(testHostName, 5, new List<string> {"192.168.0.1"});
            state.HostMxRecords = new List<HostMxRecord> {record};
            List<HostMxRecord> hostMxRecords = new List<HostMxRecord>();
            MxRecordsPolled mxRecordsPolled = new MxRecordsPolled(testDomain, hostMxRecords, TimeSpan.MinValue);

            _recordChangedNotifier.Handle(state, mxRecordsPolled);

            A.CallTo(() => _dispatcher.Dispatch(A<MxRecordRemoved>._, A<string>._))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() =>
                _dispatcher.Dispatch(A<MxRecordRemoved>.That.Matches(x => x.Id == testDomain), A<string>._)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void NotifiesWhenRecordAddedAndRemoved()
        {
            string testDomain = "domain";
            string testHostName1 = "hostname1";
            string testHostName2 = "hostname2";


            MxEntityState state = new MxEntityState(testDomain);
            HostMxRecord record1 = new HostMxRecord(testHostName1, 5, new List<string> {"192.168.0.1"});
            HostMxRecord record2 = new HostMxRecord(testHostName2, 5, new List<string> {"192.168.0.1"});

            state.HostMxRecords = new List<HostMxRecord> {record1};
            List<HostMxRecord> hostMxRecords = new List<HostMxRecord> {record2};
            MxRecordsPolled mxRecordsPolled = new MxRecordsPolled(testDomain, hostMxRecords, TimeSpan.MinValue);

            _recordChangedNotifier.Handle(state, mxRecordsPolled);

            A.CallTo(() => _dispatcher.Dispatch(A<MxRecordRemoved>._, A<string>._))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() =>
                _dispatcher.Dispatch(
                    A<MxRecordRemoved>.That.Matches(x => x.Id == testDomain && x.Records[0].Id == testHostName1),
                    A<string>._)).MustHaveHappenedOnceExactly();

            A.CallTo(() => _dispatcher.Dispatch(A<MxRecordAdded>._, A<string>._))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() =>
                _dispatcher.Dispatch(
                    A<MxRecordAdded>.That.Matches(x => x.Id == testDomain && x.Records[0].Id == testHostName2),
                    A<string>._)).MustHaveHappenedOnceExactly();
        }
    }
}