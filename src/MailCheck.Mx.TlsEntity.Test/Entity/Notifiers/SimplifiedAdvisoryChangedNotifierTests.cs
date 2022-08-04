using System;
using System.Collections.Generic;
using System.Text;
using FakeItEasy;
using MailCheck.Common.Contracts.Advisories;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Mx.TlsEntity.Config;
using MailCheck.Mx.TlsEntity.Entity;
using MailCheck.Mx.TlsEntity.Entity.Notifications;
using MailCheck.Mx.TlsEntity.Entity.Notifiers;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace MailCheck.Mx.TlsEntity.Test.Entity.Notifiers
{
    [TestFixture]
    public class SimplifiedAdvisoryChangedNotifierTests
    {
        private ITlsEntityConfig _tlsEntityConfig;
        private IMessageDispatcher _messageDispatcher;
        private ILogger<SimplifiedAdvisoryChangedNotifier<TlsFactory>> _log;
        private ISimplifiedAdvisoryChangedNotifier<TlsFactory> _changeNotifier;

        [SetUp]
        public void setup()
        {
            _tlsEntityConfig = A.Fake<ITlsEntityConfig>();
            _messageDispatcher = A.Fake<IMessageDispatcher>();
            _log = A.Fake<ILogger<SimplifiedAdvisoryChangedNotifier<TlsFactory>>>();
            _changeNotifier = new SimplifiedAdvisoryChangedNotifier<TlsFactory>(_messageDispatcher, _tlsEntityConfig, _log);
        }

        [Test]
        public void ShouldDispatchMessagesForChangseInAdvisories()
        {
            string hostname = "localhost";
            string topicArn = "topic.com";

            A.CallTo(() => _tlsEntityConfig.SnsTopicArn).Returns(topicArn);

            AdvisoryMessage message1 = new AdvisoryMessage(Guid.NewGuid(), MessageType.error, "text1", "markdown1");
            AdvisoryMessage message2 = new AdvisoryMessage(Guid.NewGuid(), MessageType.error, "text2", "markdown2");
            AdvisoryMessage message3 = new AdvisoryMessage(Guid.NewGuid(), MessageType.error, "text3", "markdown3");
            AdvisoryMessage message4 = new AdvisoryMessage(Guid.NewGuid(), MessageType.error, "text4", "markdown4");

            List<AdvisoryMessage> stateAdvisories = new List<AdvisoryMessage>() { message1, message2 };
            
            List<AdvisoryMessage> messageAdvisories = new List<AdvisoryMessage>() { message2, message3, message4 };

            List<string> domains = new List<string>() { "domain-0", "domain-1" };

            _changeNotifier.Notify(hostname, domains, stateAdvisories, messageAdvisories);

            foreach (string domain in domains)
            {
                A.CallTo(() => _messageDispatcher.Dispatch(A<TlsAdvisoryAdded>.That.Matches(_ =>
                    _.Id == domain &&
                    _.Host == hostname &&
                    _.Messages.Count == 2 &&
                    _.Messages[0].Text == "text3" &&
                    _.Messages[1].Text == "text4"), topicArn)).MustHaveHappenedOnceExactly();

                A.CallTo(() => _messageDispatcher.Dispatch(A<TlsAdvisorySustained>.That.Matches(_ =>
                    _.Id == domain &&
                    _.Host == hostname &&
                    _.Messages.Count == 1 &&
                    _.Messages[0].Text == "text2"), topicArn)).MustHaveHappenedOnceExactly();

                A.CallTo(() => _messageDispatcher.Dispatch(A<TlsAdvisoryRemoved>.That.Matches(_ =>
                    _.Id == domain &&
                    _.Host == hostname &&
                    _.Messages.Count == 1 &&
                    _.Messages[0].Text == "text1"), topicArn)).MustHaveHappenedOnceExactly();
            }
        }


        [Test]
        public void ShouldOmitSuccessAdvisories()
        {
            string hostname = "localhost";
            string topicArn = "topic.com";
            string domain = "domain-0";

            A.CallTo(() => _tlsEntityConfig.SnsTopicArn).Returns(topicArn);

            AdvisoryMessage message1 = new AdvisoryMessage(Guid.NewGuid(), MessageType.success, "text1", "markdown1");
            AdvisoryMessage message2 = new AdvisoryMessage(Guid.NewGuid(), MessageType.info, "text2", "markdown2");
            
            List<AdvisoryMessage> stateAdvisories = new List<AdvisoryMessage>() { message1, message2 };

            List<AdvisoryMessage> messageAdvisories = new List<AdvisoryMessage>() { message1, message2 };

            List<string> domains = new List<string>() { domain };

            _changeNotifier.Notify(hostname, domains, stateAdvisories, messageAdvisories);

            A.CallTo(() => _messageDispatcher.Dispatch(A<TlsAdvisorySustained>.That.Matches(_ =>
                _.Id == domain &&
                _.Host == hostname &&
                _.Messages.Count == 1 &&
                _.Messages[0].Text == "text2"), topicArn)).MustHaveHappenedOnceExactly();

            A.CallTo(() => _messageDispatcher.Dispatch(A<TlsAdvisoryRemoved>.That.Matches(_ =>
                _.Id == domain &&
                _.Host == hostname &&
                _.Messages.Count == 1 &&
                _.Messages[0].Text == "text1"), topicArn)).MustNotHaveHappened();
            
        }
    }
}
