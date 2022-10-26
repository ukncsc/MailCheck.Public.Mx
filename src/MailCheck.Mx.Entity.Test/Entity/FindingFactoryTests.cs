using System;
using FakeItEasy;
using MailCheck.Common.Contracts.Findings;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Entity.Config;
using MailCheck.Mx.Entity.Entity;
using NUnit.Framework;
using MessageType = MailCheck.Common.Contracts.Advisories.MessageType;

namespace MailCheck.Mx.Entity.Test.Entity
{
    [TestFixture]
    public class FindingFactoryTests
    {
        private FindingFactory _findingFactory;
        private IMxEntityConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = A.Fake<IMxEntityConfig>();
            _findingFactory = new FindingFactory(_config);
        }

        [TestCase(MessageType.error, "Urgent")]
        [TestCase(MessageType.info, "Informational")]
        [TestCase(MessageType.success, "Positive")]
        [TestCase(MessageType.warning, "Advisory")]
        public void MapsCorrectly(MessageType messageType, string expectedSeverity)
        {
            A.CallTo(() => _config.WebUrl).Returns("testWebUrl");
            Finding result = _findingFactory.Create(
                new NamedAdvisory(Guid.NewGuid(), "testName", messageType, "testText", "testMarkdown"),
                "testDomain", "testHost");

            Assert.AreEqual("testName", result.Name);
            Assert.AreEqual("domain:testDomain|host:testHost", result.EntityUri);
            Assert.AreEqual(expectedSeverity, result.Severity);
            Assert.AreEqual("https://testWebUrl/app/domain-security/testDomain/TLS/testHost", result.SourceUrl);
            Assert.AreEqual("testText (Host: testHost).", result.Title);
        }
    }
}