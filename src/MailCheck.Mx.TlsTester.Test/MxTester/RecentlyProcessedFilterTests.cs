using System;
using FakeItEasy;
using MailCheck.Common.Util;
using MailCheck.Mx.TlsTester.Config;
using MailCheck.Mx.TlsTester.MxTester;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace MailCheck.Mx.TlsTester.Test.MxTester
{
    [TestFixture]
    public class RecentlyProcessedLedgerTests
    {
        private RecentlyProcessedLedger _recentlyProcessedLedger;
        private IClock _clock;
        private IMxTesterConfig _mxTesterConfig;
        private ILogger<RecentlyProcessedLedger> _log;

        const int ValidityPeriod = 10;

        [SetUp]
        public void SetUp()
        {
            _clock = A.Fake<IClock>();
            _log = A.Fake<ILogger<RecentlyProcessedLedger>>();
            _mxTesterConfig = A.Fake<IMxTesterConfig>();
            A.CallTo(() => _mxTesterConfig.TlsTesterHostRetestPeriodSeconds).Returns(ValidityPeriod);

            _recentlyProcessedLedger = new RecentlyProcessedLedger(_clock, _mxTesterConfig, _log);
        }

        [Test]
        public void ContainsIsPositiveForItemInsideValidityPeriod()
        {
            A.CallTo(() => _clock.GetDateTimeUtc()).Returns(DateTime.UnixEpoch);
            _recentlyProcessedLedger.Set("testHost");

            A.CallTo(() => _clock.GetDateTimeUtc()).Returns(DateTime.UnixEpoch.AddSeconds(ValidityPeriod - 1));
            Assert.True(_recentlyProcessedLedger.Contains("testHost"));
        }

        [Test]
        public void ContainsIsNegativeForItemOutsideValidityPeriod()
        {
            A.CallTo(() => _clock.GetDateTimeUtc()).Returns(DateTime.UnixEpoch);
            _recentlyProcessedLedger.Set("testHost");

            A.CallTo(() => _clock.GetDateTimeUtc()).Returns(DateTime.UnixEpoch.AddSeconds(ValidityPeriod + 1));
            Assert.False(_recentlyProcessedLedger.Contains("testHost"));
        }

        [Test]
        public void ContainsIsNegativeForItemNotAdded()
        {
            A.CallTo(() => _clock.GetDateTimeUtc()).Returns(DateTime.UnixEpoch);

            Assert.False(_recentlyProcessedLedger.Contains("testHost"));
        }

        [Test]
        public void ContainsIsPositiveForSlidingValidityPeriod()
        {
            A.CallTo(() => _clock.GetDateTimeUtc()).Returns(DateTime.UnixEpoch);
            _recentlyProcessedLedger.Set("testHost");

            A.CallTo(() => _clock.GetDateTimeUtc()).Returns(DateTime.UnixEpoch.AddSeconds(10));
            _recentlyProcessedLedger.Set("testHost");

            A.CallTo(() => _clock.GetDateTimeUtc()).Returns(DateTime.UnixEpoch.AddSeconds(ValidityPeriod + 9));
            Assert.True(_recentlyProcessedLedger.Contains("testHost"));
        }
    }
}
