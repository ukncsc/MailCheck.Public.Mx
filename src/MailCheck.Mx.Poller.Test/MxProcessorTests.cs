using System.Collections.Generic;
using System.Threading.Tasks;
using FakeItEasy;
using MailCheck.Mx.Contracts.Poller;
using MailCheck.Mx.Poller.Config;
using MailCheck.Mx.Poller.Dns;
using MailCheck.Mx.Poller.Domain;
using MailCheck.Mx.Poller.Exception;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace MailCheck.Mx.Poller.Test
{
    [TestFixture]
    public class MxProcessorTests
    {
        private IDnsClient _dnsClient;
        private IMxProcessor _mxProcessor;
        private IMxPollerConfig _config;
        private ILogger<MxProcessor> _log;

        [SetUp]
        public void SetUp()
        {
            _dnsClient = A.Fake<IDnsClient>();
            _config = A.Fake<IMxPollerConfig>();
            _log = A.Fake<ILogger<MxProcessor>>();

            _mxProcessor = new MxProcessor(_dnsClient, _config, _log);
        }

        [Test]
        public async Task MxExceptionThrownWhenAllowNullResultsNotSetAndEmptyResult()
        {
            string domain = "abc.com";

            A.CallTo(() => _config.AllowNullResults).Returns(false);

            Assert.Throws<MxPollerException>(() => _mxProcessor.Process(domain).GetAwaiter().GetResult());
        }

        [Test]
        public async Task MxExceptionNotThrownWhenAllowNullResultsSetAndEmptyResult()
        {
            string domain = "abc.com";

            A.CallTo(() => _config.AllowNullResults).Returns(true);

            MxPollResult result = await _mxProcessor.Process(domain);

            Assert.AreEqual(0, result.Records.Count);
        }

        [Test]
        public async Task ErroredWhenRetrievingMxRecordTest()
        {
            string domain = "abc.com";

            A.CallTo(() => _config.AllowNullResults).Returns(true);

            A.CallTo(() => _dnsClient.GetMxRecords(A<string>._))
                .Returns(new DnsResult<List<HostMxRecord>>("error"));

            MxPollResult result = await _mxProcessor.Process(domain);
            Assert.That(domain, Is.EqualTo(result.Id));
        }
    }
}