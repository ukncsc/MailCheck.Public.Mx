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
    public class MxSecurityTesterIgnoredHostsFilterTests
    {
        private MxSecurityTesterIgnoredHostsFilter _ignoredHostsFilter;
        private ILogger<MxSecurityTesterIgnoredHostsFilter> _log;
        private IMxTesterConfig _config;


        [SetUp]
        public void SetUp()
        {
            _log = A.Fake<ILogger<MxSecurityTesterIgnoredHostsFilter>>();
            _config = A.Fake<IMxTesterConfig>();

            A.CallTo(() => _config.TlsTesterIgnoredHosts).Returns(new string[] {"com.mimecast.service"});


            _ignoredHostsFilter = new MxSecurityTesterIgnoredHostsFilter(
               _log,
               _config
               );
        }

        [Test]
        public void AHostOnTheIgnoredList_ShouldNotBeIncluded_TlsTesting()
        {
            string badhost = "service333.mimecast.com";

            bool hostStatus = _ignoredHostsFilter.IsIgnored(badhost);
            Assert.IsTrue(hostStatus, "IsIgnored in testing is true for hosts ignored");
        }

        [Test]
        public void AHostNotIgnored_ShouldBeIncluded_TlsTesting()
        {
            string goodHost = "ncsc.gov.uk";

            bool hostStatus = _ignoredHostsFilter.IsIgnored(goodHost);
            Assert.IsFalse(hostStatus, "IsIgnored in testing is false for hosts included");
        }

    }
}
