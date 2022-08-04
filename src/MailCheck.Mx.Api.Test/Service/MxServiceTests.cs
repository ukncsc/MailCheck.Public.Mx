using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FakeItEasy;
using MailCheck.Common.Contracts.Messaging;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Mx.Api.Config;
using MailCheck.Mx.Api.Dao;
using MailCheck.Mx.Api.Domain;
using MailCheck.Mx.Api.Service;
using MailCheck.Mx.Contracts.Entity;
using MailCheck.Mx.Contracts.External;
using MailCheck.Mx.Contracts.Poller;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Simplified;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace MailCheck.Mx.Api.Test.Service
{
    [TestFixture]
    public class MxServiceTests
    {
        private MxService _mxService;
        private IMxApiDao _mxApiDao;
        private IDomainTlsEvaluatorResultsFactory _domainTlsEvaluatorResultsFactory;
        private IMessagePublisher _messagePublisher;
        private IMxApiConfig _config;
        private ILogger<MxService> _logger;

        [SetUp]
        public void SetUp()
        {
            _mxApiDao = A.Fake<IMxApiDao>();
            _domainTlsEvaluatorResultsFactory = A.Fake<IDomainTlsEvaluatorResultsFactory>();
            _messagePublisher = A.Fake<IMessagePublisher>();
            _config = A.Fake<IMxApiConfig>();
            _logger = A.Fake<ILogger<MxService>>();
            A.CallTo(() => _config.MicroserviceOutputSnsTopicArn).Returns("SnsTopicArn");
            _mxService = new MxService(_mxApiDao, _domainTlsEvaluatorResultsFactory, _messagePublisher, _config, _logger);
        }

        [Test]
        public async Task MissingDomainPublished()
        {
            A.CallTo(() => _mxApiDao.GetMxEntityState("testDomain")).Returns((MxEntityState)null);

            DomainTlsEvaluatorResults result = await _mxService.GetDomainTlsEvaluatorResults("testDomain");

            A.CallTo(() => _messagePublisher.Publish(A<DomainMissing>.That.Matches(x => x.Id == "testDomain"), "SnsTopicArn")).MustHaveHappenedOnceExactly();
            Assert.Null(result);
        }

        [Test]
        public async Task MissingHostMxRecordsReturnPending()
        {
            MxEntityState mxStateFromDao = new MxEntityState("");
            DomainTlsEvaluatorResults pendingEvaluatorResultFromFactory = new DomainTlsEvaluatorResults("", true, true);

            A.CallTo(() => _mxApiDao.GetMxEntityState("testDomain")).Returns(mxStateFromDao);
            A.CallTo(() => _domainTlsEvaluatorResultsFactory.CreatePending("testDomain")).Returns(pendingEvaluatorResultFromFactory);

            DomainTlsEvaluatorResults result = await _mxService.GetDomainTlsEvaluatorResults("testDomain");

            A.CallTo(() => _messagePublisher.Publish(A<DomainMissing>._, A<string>._)).MustNotHaveHappened();
            Assert.AreSame(pendingEvaluatorResultFromFactory, result);
        }

        [Test]
        public async Task EvaluatorResultsAreReturned()
        {
            var simplifiedStatesFromDao = new List<SimplifiedTlsEntityState>
            {
                new SimplifiedTlsEntityState("testHostname", "testIpAddress")
                {
                    TlsAdvisories = new List<NamedAdvisory>(),
                }
            };

            DomainTlsEvaluatorResults evaluatorResultFromFactory = new DomainTlsEvaluatorResults("testDomain", false, true);

            A.CallTo(() => _mxApiDao.GetSimplifiedStates("testDomain")).Returns(simplifiedStatesFromDao);
            A.CallTo(() => _domainTlsEvaluatorResultsFactory.Create("testDomain", A<Dictionary<string, int>>._, A<List<SimplifiedTlsEntityState>>._))
                .Returns(evaluatorResultFromFactory);

            DomainTlsEvaluatorResults result = await _mxService.GetDomainTlsEvaluatorResults("testDomain");

            Assert.AreEqual(evaluatorResultFromFactory, result);
        }

        [Test]
        public async Task EvaluatorResultsAreReturnedForTlsRequired()
        {
            MxEntityState mxStateFromDao = new MxEntityState("")
            {
                HostMxRecords = new List<HostMxRecord> { new HostMxRecord("testHost1", 0, null) }
            };

            DomainTlsEvaluatorResults evaluatorResultFromFactory = new DomainTlsEvaluatorResults("", false, true);

            A.CallTo(() => _mxApiDao.GetMxEntityState("testDomain")).Returns(mxStateFromDao);
            A.CallTo(() => _domainTlsEvaluatorResultsFactory.CreateNoTls("testDomain")).Returns(evaluatorResultFromFactory);

            DomainTlsEvaluatorResults result = await _mxService.GetDomainTlsEvaluatorResults("testDomain");

            A.CallTo(() => _messagePublisher.Publish(A<DomainMissing>._, A<string>._)).MustNotHaveHappened();
            Assert.AreEqual(evaluatorResultFromFactory, result);
        }

        [Test]
        public async Task SimplifiedResultsAreReturnedIfNotEmpty()
        {
            var simplifiedStatesFromDao = new List<SimplifiedTlsEntityState>
            {
                new SimplifiedTlsEntityState("testHostname", "testIpAddress")
            };

            DomainTlsEvaluatorResults evaluatorResultFromFactory = new DomainTlsEvaluatorResults("testDomain", false, true);

            A.CallTo(() => _mxApiDao.GetSimplifiedStates("testDomain")).Returns(simplifiedStatesFromDao);
            A.CallTo(() => _domainTlsEvaluatorResultsFactory.Create("testDomain", A<Dictionary<string, int>>._, simplifiedStatesFromDao))
                .Returns(evaluatorResultFromFactory);

            var result = await _mxService.GetDomainTlsEvaluatorResults("testDomain");

            Assert.AreEqual(evaluatorResultFromFactory, result);
        }

        [Test]
        public async Task SimplifiedResultsAreReturnedIfAvailable()
        {
            var simplifiedStatesFromDao = new List<SimplifiedTlsEntityState>
            {
                new SimplifiedTlsEntityState("testHostname", "testIpAddress")
                {
                    TlsAdvisories = new List<NamedAdvisory>(),
                }
            };

            A.CallTo(() => _mxApiDao.GetSimplifiedStates("testDomain")).Returns(simplifiedStatesFromDao);

            Dictionary<string, int> mxHostPreferences = new Dictionary<string, int>();
            A.CallTo(() => _mxApiDao.GetPreferences("testDomain")).Returns(mxHostPreferences);

            var resultsFromEvaluator = new DomainTlsEvaluatorResults(null, false, true);
            A.CallTo(() => _domainTlsEvaluatorResultsFactory.Create("testDomain", mxHostPreferences, A<List<SimplifiedTlsEntityState>>.That.IsSameSequenceAs(simplifiedStatesFromDao))).Returns(resultsFromEvaluator);

            var result = await _mxService.GetDomainTlsEvaluatorResults("testDomain");

            Assert.AreSame(resultsFromEvaluator, result);
        }

        [Test]
        public async Task RecheckTlsIsDispatched()
        {
            var simplifiedStatesFromDao = new List<SimplifiedTlsEntityState>
            {
                new SimplifiedTlsEntityState("testHostname", "testIpAddress1")
                {
                    TlsLastUpdated = DateTime.MinValue,
                    CertsLastUpdated = DateTime.MinValue,
                },
                new SimplifiedTlsEntityState("testHostname", "testIpAddress2")
                {
                    TlsLastUpdated = DateTime.MinValue,
                    CertsLastUpdated = DateTime.MinValue,
                }
            };

            var ipStates = new List<IpState>
            {
                new IpState("testIpAddress1", DateTime.MinValue, DateTime.MinValue),
                new IpState("testIpAddress2", DateTime.MinValue, DateTime.MinValue)
            };

            DomainTlsEvaluatorResults evaluatorResultFromFactory = new DomainTlsEvaluatorResults("testDomain", false, true, null, null, ipStates);

            A.CallTo(() => _mxApiDao.GetSimplifiedStates("testDomain")).Returns(simplifiedStatesFromDao);
            A.CallTo(() => _domainTlsEvaluatorResultsFactory.Create("testDomain", A<Dictionary<string, int>>._, A<List<SimplifiedTlsEntityState>>._))
                .Returns(evaluatorResultFromFactory);

            A.CallTo(() => _config.RecheckMinPeriodInSeconds).Returns(300);
            A.CallTo(() => _config.SnsTopicArn).Returns("testTopic");

            bool result = await _mxService.RecheckTls("testDomain");

            Assert.IsTrue(result);

            A.CallTo(() => _messagePublisher.Publish(A<SimplifiedTlsExpired>.That.Matches(x => x.ResourceId == "testIpAddress1"),
                A<string>.That.Matches(x => x == "testTopic"))).MustHaveHappenedOnceExactly();

            A.CallTo(() => _messagePublisher.Publish(A<SimplifiedTlsExpired>.That.Matches(x => x.ResourceId == "testIpAddress2"),
                A<string>.That.Matches(x => x == "testTopic"))).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task RecheckTlsIsNotDispatched()
        {
            var simplifiedStatesFromDao = new List<SimplifiedTlsEntityState>
            {
                new SimplifiedTlsEntityState("testHostname", "testIpAddress1")
                {
                    TlsLastUpdated = DateTime.MinValue,
                    CertsLastUpdated = DateTime.MinValue,
                },
                new SimplifiedTlsEntityState("testHostname", "testIpAddress2")
                {
                    TlsLastUpdated = DateTime.MaxValue,
                    CertsLastUpdated = DateTime.MaxValue,
                }
            };

            var ipStates = new List<IpState>
            {
                new IpState("testIpAddress1", DateTime.MinValue, DateTime.MinValue),
                new IpState("testIpAddress2", DateTime.MaxValue, DateTime.MaxValue)
            };

            DomainTlsEvaluatorResults evaluatorResultFromFactory = new DomainTlsEvaluatorResults("testDomain", false, true, null, null, ipStates);

            A.CallTo(() => _mxApiDao.GetSimplifiedStates("testDomain")).Returns(simplifiedStatesFromDao);
            A.CallTo(() => _domainTlsEvaluatorResultsFactory.Create("testDomain", A<Dictionary<string, int>>._, A<List<SimplifiedTlsEntityState>>._))
                .Returns(evaluatorResultFromFactory);

            A.CallTo(() => _config.RecheckMinPeriodInSeconds).Returns(300);
            A.CallTo(() => _config.SnsTopicArn).Returns("testTopic");

            bool result = await _mxService.RecheckTls("testDomain");

            Assert.IsFalse(result);

            A.CallTo(() => _messagePublisher.Publish(A<SimplifiedTlsExpired>.That.Matches(x => x.ResourceId == "testIpAddress1"),
                A<string>.That.Matches(x => x == "testTopic"))).MustNotHaveHappened();

            A.CallTo(() => _messagePublisher.Publish(A<SimplifiedTlsExpired>.That.Matches(x => x.ResourceId == "testIpAddress2"),
                A<string>.That.Matches(x => x == "testTopic"))).MustNotHaveHappened();
        }
    }
}
