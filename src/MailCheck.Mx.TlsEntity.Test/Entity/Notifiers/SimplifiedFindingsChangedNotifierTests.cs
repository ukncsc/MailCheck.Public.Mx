using System;
using System.Collections.Generic;
using System.Text;
using FakeItEasy;
using MailCheck.Common.Contracts.Advisories;
using MailCheck.Common.Contracts.Findings;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Common.Processors.Notifiers;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.TlsEntity.Config;
using MailCheck.Mx.TlsEntity.Entity;
using MailCheck.Mx.TlsEntity.Entity.Notifications;
using MailCheck.Mx.TlsEntity.Entity.Notifiers;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace MailCheck.Mx.TlsEntity.Test.Entity.Notifiers
{
    [TestFixture]
    public class SimplifiedFindingsChangedNotifierTests
    {
        private ITlsEntityConfig _tlsEntityConfig;
        private IMessageDispatcher _messageDispatcher;
        private ILogger<SimplifiedFindingsChangedNotifier> _log;
        private FindingsChangedNotifier _findingsChangedNotifier;
        private SimplifiedFindingsChangedNotifier _changeNotifier;

        [SetUp]
        public void setup()
        {
            _tlsEntityConfig = A.Fake<ITlsEntityConfig>();
            _messageDispatcher = A.Fake<IMessageDispatcher>();
            _log = A.Fake<ILogger<SimplifiedFindingsChangedNotifier>>();
            _findingsChangedNotifier = A.Fake<FindingsChangedNotifier>();
            _changeNotifier = new SimplifiedFindingsChangedNotifier(_messageDispatcher, _tlsEntityConfig, _findingsChangedNotifier, _log);
        }

        [Test]
        public void ShouldAppendHostToFindingText()
        {
            string host = "test.host.com";
            List<string> domains = new List<string> { "test.domain.com" };
            List<NamedAdvisory> currentAdvisories = new List<NamedAdvisory>();
            List<NamedAdvisory> newAdvisories = new List<NamedAdvisory>
            {
                new NamedAdvisory(new Guid(), "mailcheck.tls.testname1", Common.Contracts.Advisories.MessageType.error, "append here:", "markdown")
            };

            _changeNotifier.Handle(host, domains, "TLS", currentAdvisories, newAdvisories);

            A.CallTo(() => _messageDispatcher.Dispatch(A<FindingsChanged>.That.Matches(x =>
                x.Domain == "test.domain.com" &&
                x.Added[0].Title == "append here: (Host: test.host.com)."), A<string>._)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void MultipleDomainsAndHostsShouldOnlyAppendOnce()
        {
            string host = "test.host.com";
            List<string> domains = new List<string> { "test.domain.com", "test2.domain.com", "test3.domain.com" };
            List<NamedAdvisory> currentAdvisories = new List<NamedAdvisory>();
            List<NamedAdvisory> newAdvisories = new List<NamedAdvisory>
            {
                new NamedAdvisory(new Guid(), "mailcheck.tls.testname1", Common.Contracts.Advisories.MessageType.error, "append here1:", "markdown"),
                new NamedAdvisory(new Guid(), "mailcheck.tls.testname2", Common.Contracts.Advisories.MessageType.error, "append here2:", "markdown"),
                new NamedAdvisory(new Guid(), "mailcheck.tls.testname3", Common.Contracts.Advisories.MessageType.error, "append here3:", "markdown")
            };

            _changeNotifier.Handle(host, domains, "TLS", currentAdvisories, newAdvisories);

            A.CallTo(() => _messageDispatcher.Dispatch(A<FindingsChanged>.That.Matches(x =>
                x.Domain == "test.domain.com" &&
                x.Added[0].Title == "append here1: (Host: test.host.com)." &&
                x.Added[1].Title == "append here2: (Host: test.host.com)." &&
                x.Added[2].Title == "append here3: (Host: test.host.com)."), A<string>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _messageDispatcher.Dispatch(A<FindingsChanged>.That.Matches(x =>
                x.Domain == "test2.domain.com" &&
                x.Added[0].Title == "append here1: (Host: test.host.com)." &&
                x.Added[1].Title == "append here2: (Host: test.host.com)." &&
                x.Added[2].Title == "append here3: (Host: test.host.com)."), A<string>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _messageDispatcher.Dispatch(A<FindingsChanged>.That.Matches(x =>
                x.Domain == "test3.domain.com" &&
                x.Added[0].Title == "append here1: (Host: test.host.com)." &&
                x.Added[1].Title == "append here2: (Host: test.host.com)." &&
                x.Added[2].Title == "append here3: (Host: test.host.com)."), A<string>._)).MustHaveHappenedOnceExactly();
        }
    }
}
