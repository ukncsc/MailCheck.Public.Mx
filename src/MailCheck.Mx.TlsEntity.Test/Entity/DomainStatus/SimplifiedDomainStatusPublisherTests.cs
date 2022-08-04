using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FakeItEasy;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.DomainStatus.Contracts;
using MailCheck.Mx.TlsEntity.Config;
using MailCheck.Mx.TlsEntity.Dao;
using MailCheck.Mx.TlsEntity.Entity.DomainStatus;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace MailCheck.Mx.TlsEntity.Test.Entity.DomainStatus
{
    [TestFixture]
    public class SimplifiedDomainStatusPublisherTests
    {
        [Test]
        public async Task Test()
        {
            var messageDispatcher = A.Fake<IMessageDispatcher>();
            var tlsEntityConfig = A.Fake<ITlsEntityConfig>();
            var simplifiedTlsEntityDao = A.Fake<ISimplifiedTlsEntityDao>();
            var logger = A.Fake<ILogger<SimplifiedDomainStatusPublisher>>();
            var publisher = new SimplifiedDomainStatusPublisher(messageDispatcher, tlsEntityConfig, simplifiedTlsEntityDao, logger);

            A.CallTo(() => tlsEntityConfig.RecordType).Returns("tls");
            A.CallTo(() => tlsEntityConfig.SnsTopicArn).Returns("topicArn");

            string ipAddress = "ipaddress";

            var blah = new Dictionary<string, Status> 
            { 
                ["domain.com"] = Status.Error,
                ["domain2.com"] = Status.Warning
            };

            A.CallTo(() => simplifiedTlsEntityDao.GetMaxAdvisoryStatusesForAffectedDomainsByMxHostIp(ipAddress)).Returns(blah);
            
            await publisher.CalculateAndPublishDomainStatuses(ipAddress);

            A.CallTo(() => messageDispatcher.Dispatch(
                A<DomainStatusEvaluation>.That.Matches(dse => dse.Id == "domain.com" && dse.RecordType == "tls" && dse.Status == Status.Error),
                "topicArn")).MustHaveHappened();
            A.CallTo(() => messageDispatcher.Dispatch(
                A<DomainStatusEvaluation>.That.Matches(dse => dse.Id == "domain2.com" && dse.RecordType == "tls" && dse.Status == Status.Warning),
                "topicArn")).MustHaveHappened();
        }
    }
}
