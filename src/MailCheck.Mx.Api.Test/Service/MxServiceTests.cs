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
using MailCheck.Mx.Contracts.Poller;
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
            DomainTlsEvaluatorResults pendingEvaluatorResultFromFactory = new DomainTlsEvaluatorResults("", true);

            A.CallTo(() => _mxApiDao.GetMxEntityState("testDomain")).Returns(mxStateFromDao);
            A.CallTo(() => _domainTlsEvaluatorResultsFactory.CreatePending("testDomain")).Returns(pendingEvaluatorResultFromFactory);

            DomainTlsEvaluatorResults result = await _mxService.GetDomainTlsEvaluatorResults("testDomain");

            A.CallTo(() => _messagePublisher.Publish(A<DomainMissing>._, A<string>._)).MustNotHaveHappened();
            Assert.AreSame(pendingEvaluatorResultFromFactory, result);
        }

        [Test]
        public async Task EvaluatorResultsAreReturned()
        {
            MxEntityState mxStateFromDao = new MxEntityState("")
            {
                HostMxRecords = new List<HostMxRecord> { new HostMxRecord("testHost1", 0, null) }
            };

            Dictionary<string, TlsEntityState> tlsEntityStatesFromDao = new Dictionary<string, TlsEntityState>();
            DomainTlsEvaluatorResults evaluatorResultFromFactory = new DomainTlsEvaluatorResults("", false);

            A.CallTo(() => _mxApiDao.GetMxEntityState("testDomain")).Returns(mxStateFromDao);
            A.CallTo(() => _mxApiDao.GetTlsEntityStates(A<List<string>>._)).Returns(tlsEntityStatesFromDao);
            A.CallTo(() => _domainTlsEvaluatorResultsFactory.Create(mxStateFromDao, tlsEntityStatesFromDao)).Returns(evaluatorResultFromFactory);

            DomainTlsEvaluatorResults result = await _mxService.GetDomainTlsEvaluatorResults("testDomain");

            A.CallTo(() => _messagePublisher.Publish(A<DomainMissing>._, A<string>._)).MustNotHaveHappened();
            Assert.AreSame(evaluatorResultFromFactory, result);
        }
    }
}
