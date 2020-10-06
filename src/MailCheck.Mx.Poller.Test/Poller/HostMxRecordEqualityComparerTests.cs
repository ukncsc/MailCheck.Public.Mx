using System.Collections.Generic;
using MailCheck.Mx.Contracts.Poller;
using NUnit.Framework;

namespace MailCheck.Mx.Poller.Test.Poller
{
    [TestFixture]
    public class HostMxRecordTests
    {

        private HostMxRecord GetMxRecord(int id)
        {
            return new HostMxRecord("hostName" + id, id, new List<string> { "ipAddress" + id });
        }

        [Test]
        public void ShouldBeEqualWhenSameValues()
        {
            var hostMxRecordA = new HostMxRecord("hostName1", 1, new List<string> { "ipAddress1" });
            var hostMxRecordB = new HostMxRecord("hostName1", 1, new List<string> { "ipAddress1" });

            Assert.AreEqual(hostMxRecordA, hostMxRecordB);
        }

        [Test]
        public void ShouldBeEqualWhenSameIpAddressesInDifferentOrder()
        {
            var hostMxRecordA = new HostMxRecord("hostName1", 1, new List<string> { "ipAddress1", "ipAddress2" });
            var hostMxRecordB = new HostMxRecord("hostName1", 1, new List<string> { "ipAddress2", "ipAddress1" });

            Assert.AreEqual(hostMxRecordA, hostMxRecordB);
        }

        [Test]
        public void ShouldNotBeEqualWhenIpAddressesDifferent()
        {
            var hostMxRecordA = new HostMxRecord("hostName1", 1, new List<string> { "ipAddress1", "ipAddress2" });
            var hostMxRecordB = new HostMxRecord("hostName1", 1, new List<string> { "ipAddress2", "ipAddress3" });

            Assert.AreNotEqual(hostMxRecordA, hostMxRecordB);
        }

        [Test]
        public void ShouldNotBeEqualWhenHostNameDifferent()
        {
            var hostMxRecordA = new HostMxRecord("hostName1", 1, new List<string>());
            var hostMxRecordB = new HostMxRecord("hostName2", 1, new List<string>());

            Assert.AreNotEqual(hostMxRecordA, hostMxRecordB);
        }

        [Test]
        public void ShouldNotBeEqualWhenPreferenceDifferent()
        {
            var hostMxRecordA = new HostMxRecord("hostName1", 1, new List<string>());
            var hostMxRecordB = new HostMxRecord("hostName1", 2, new List<string>());

            Assert.AreNotEqual(hostMxRecordA, hostMxRecordB);
        }

        [Test]
        public void ShouldNotBeEqualWhenIpAddressMissing1()
        {
            var hostMxRecordA = new HostMxRecord("hostName1", 1, new List<string> { "ipAddress1" });
            var hostMxRecordB = new HostMxRecord("hostName1", 1, new List<string>());

            Assert.AreNotEqual(hostMxRecordA, hostMxRecordB);
        }

        [Test]
        public void ShouldNotBeEqualWhenIpAddressMissing2()
        {
            var hostMxRecordA = new HostMxRecord("hostName1", 1, new List<string>());
            var hostMxRecordB = new HostMxRecord("hostName1", 1, new List<string> { "ipAddress1" });

            Assert.AreNotEqual(hostMxRecordA, hostMxRecordB);
        }

        [Test]
        public void ShouldNotBeEqualWhenIpAddressMissing3()
        {
            var hostMxRecordA = new HostMxRecord("hostName1", 1, new List<string> { "ipAddress1" });
            var hostMxRecordB = new HostMxRecord("hostName1", 1, null);

            Assert.AreNotEqual(hostMxRecordA, hostMxRecordB);
        }

        [Test]
        public void ShouldNotBeEqualWhenIpAddressMissing4()
        {
            var hostMxRecordA = new HostMxRecord("hostName1", 1, null);
            var hostMxRecordB = new HostMxRecord("hostName1", 1, new List<string> { "ipAddress1" });

            Assert.AreNotEqual(hostMxRecordA, hostMxRecordB);
        }

        [Test]
        public void ShouldBeEqualWhenHostNameSameButContainsSpacesAfterSemiColon()
        {
            var hostMxRecordA = new HostMxRecord("hostName1; ", 1, new List<string>());
            var hostMxRecordB = new HostMxRecord("hostName1;", 1, new List<string>());

            Assert.AreEqual(hostMxRecordA, hostMxRecordB);
        }

        [Test]
        public void ShouldBeEqualWhenBothNull()
        {
            var hostMxRecordA = (HostMxRecord)null;
            var hostMxRecordB = (HostMxRecord)null;

            Assert.AreEqual(hostMxRecordA, hostMxRecordB);
        }

        [Test]
        public void ShouldBeEqualWhenSameValuesButHostNameNull()
        {
            var hostMxRecordA = new HostMxRecord(null, 1, new List<string> { "ipAddress1" });
            var hostMxRecordB = new HostMxRecord(null, 1, new List<string> { "ipAddress1" });

            Assert.AreEqual(hostMxRecordA, hostMxRecordB);
        }

        [Test]
        public void ShouldBeEqualWhenSameValuesButPreferenceNull()
        {
            var hostMxRecordA = new HostMxRecord("hostName1", null, new List<string> { "ipAddress1" });
            var hostMxRecordB = new HostMxRecord("hostName1", null, new List<string> { "ipAddress1" });

            Assert.AreEqual(hostMxRecordA, hostMxRecordB);
        }

        [Test]
        public void ShouldBeEqualWhenSameValuesButIpAddressesNull()
        {
            var hostMxRecordA = new HostMxRecord("hostName1", 1, null);
            var hostMxRecordB = new HostMxRecord("hostName1", 1, null);

            Assert.AreEqual(hostMxRecordA, hostMxRecordB);
        }
    }
}
