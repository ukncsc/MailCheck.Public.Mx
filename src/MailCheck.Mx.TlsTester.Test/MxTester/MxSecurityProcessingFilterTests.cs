using FakeItEasy;
using MailCheck.Mx.TlsTester.Config;
using MailCheck.Mx.TlsTester.MxTester;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace MailCheck.Mx.TlsTester.Test.MxTester
{
    [TestFixture]
    public class MxSecurityProcessingFilterTests
    {
        MxSecurityProcessingFilter _filter;

        [SetUp]
        public void SetUp()
        {
            var logger = A.Fake<ILogger<MxSecurityProcessingFilter>>();
            var config = A.Fake<IMxTesterConfig>();

            _filter = new MxSecurityProcessingFilter(logger);
        }

        [Test]
        public void Reserve_WithEmpty_ShouldSucceed()
        {
            var success1 = _filter.Reserve("mx.ncsc.gov.uk");

            Assert.IsTrue(success1, "First reserve of host should succeed");
            Assert.AreEqual(1, _filter.HostCount, "Should be one host in filter");
        }

        [Test]
        public void ReleaseReservation_WithEmpty_ShouldSucceed()
        {
            _filter.ReleaseReservation("mx.ncsc.gov.uk");
            Assert.AreEqual(0, _filter.HostCount, "Should be zero host in filter");
        }

        [Test]
        public void Reserve_WithDifferentHosts_ShouldSucceed()
        {
            var success1 = _filter.Reserve("mx.ncsc.gov.uk");
            var success2 = _filter.Reserve("mx.gchq.gov.uk");

            Assert.IsTrue(success1, "First reserve of host should succeed");
            Assert.IsTrue(success2, "Second reserve of different host should succeed");
            Assert.AreEqual(2, _filter.HostCount, "Should be two hosts in filter");
        }

        [Test]
        public void Reserve_WithDifferentHostsAndRelease_ShouldSucceed()
        {
            var success1 = _filter.Reserve("mx.ncsc.gov.uk");
            var success2 = _filter.Reserve("mx.gchq.gov.uk");
            _filter.ReleaseReservation("mx.ncsc.gov.uk");

            Assert.IsTrue(success1, "First reserve of host should succeed");
            Assert.IsTrue(success2, "Second reserve of different host should succeed");
            Assert.AreEqual(1, _filter.HostCount, "Should be one host in filter");
        }

        [Test]
        public void Reserve_RepeatReserve_ShouldFail()
        {
            var success1 = _filter.Reserve("mx.ncsc.gov.uk");
            var success2 = _filter.Reserve("mx.ncsc.gov.uk");
            _filter.ReleaseReservation("mx.ncsc.gov.uk");
            var success3 = _filter.Reserve("mx.ncsc.gov.uk");

            Assert.IsTrue(success1, "First reserve of host should succeed");
            Assert.IsFalse(success2, "Second reserve of host should fail");
            Assert.IsTrue(success3, "Third reserve of host, after the release, should succeed");
            Assert.AreEqual(1, _filter.HostCount, "Should be one host in filter");
        }

        [Test]
        public void Reserve_ReserveAndReleaseDifferentHosts_ShouldWork()
        {
            var success1 = _filter.Reserve("mx.ncsc.gov.uk");
            var success2 = _filter.Reserve("mx.gchq.gov.uk");
            _filter.ReleaseReservation("mx.ncsc.gov.uk");
            _filter.ReleaseReservation("mx.gchq.gov.uk");

            Assert.IsTrue(success1, "First reserve of host should succeed");
            Assert.IsTrue(success2, "Second reserve of host should succeed");
            Assert.AreEqual(0, _filter.HostCount, "Should be zero hosts in filter");
        }
    }
}
