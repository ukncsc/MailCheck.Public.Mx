using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using MailCheck.Common.Contracts.Advisories;
using MailCheck.Common.Contracts.Messaging;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Mx.Contracts.External;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Simplified;
using MailCheck.Mx.TlsEntity.Config;
using MailCheck.Mx.TlsEntity.Dao;
using MailCheck.Mx.TlsEntity.Entity;
using MailCheck.Mx.TlsEntity.Entity.DomainStatus;
using MailCheck.Mx.TlsEntity.Entity.Notifiers;
using MailCheck.Mx.TlsEntity.Entity.EmailSecurity;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using MessageType = MailCheck.Common.Contracts.Advisories.MessageType;

namespace MailCheck.Mx.TlsEntity.Test
{
    [TestFixture]
    public class SimplifiedTlsEntityTests
    {
        private ITlsEntityConfig _tlsEntityConfig;
        private IMessageDispatcher _dispatcher;
        private ILogger<SimplifiedTlsEntity> _log;
        private SimplifiedTlsEntity _simplifiedTlsEntity;
        private ISimplifiedTlsEntityDao _hostnameIpAddressDao;
        private ISimplifiedAdvisoryChangedNotifier<TlsFactory> _tlsChangeNotifier;
        private ISimplifiedAdvisoryChangedNotifier<CertFactory> _certChangeNotifier;
        private ISimplifiedFindingsChangedNotifier _findingsChangedNotifier;
        private ISimplifiedEntityChangedPublisher _entityChangedPublisher;
        private ISimplifiedDomainStatusPublisher _domainStatusPublisher;

        [SetUp]
        public void setup()
        {
            _tlsEntityConfig = A.Fake<ITlsEntityConfig>();
            _dispatcher = A.Fake<IMessageDispatcher>();
            _log = A.Fake<ILogger<SimplifiedTlsEntity>>();
            _hostnameIpAddressDao = A.Fake<ISimplifiedTlsEntityDao>();
            _tlsChangeNotifier = A.Fake<ISimplifiedAdvisoryChangedNotifier<TlsFactory>>();
            _certChangeNotifier = A.Fake<ISimplifiedAdvisoryChangedNotifier<CertFactory>>();
            _findingsChangedNotifier = A.Fake<ISimplifiedFindingsChangedNotifier>();
            _entityChangedPublisher = A.Fake<ISimplifiedEntityChangedPublisher>();

            _domainStatusPublisher = A.Fake<ISimplifiedDomainStatusPublisher>();

            _simplifiedTlsEntity = new SimplifiedTlsEntity(
               _tlsEntityConfig,
                _dispatcher,
                _log,
                _hostnameIpAddressDao,
                _tlsChangeNotifier,
                _certChangeNotifier,
                _findingsChangedNotifier,
                _entityChangedPublisher,
                _domainStatusPublisher
            );
        }

        [Test]
        public async Task Handle_SimplifiedTlsScheduledReminder_ShouldDispatchSimplifiedTlsTestPending()
        {
            Guid id = Guid.NewGuid();
            string ip = "127.0.0.1";
            SimplifiedTlsScheduledReminder reminder = new SimplifiedTlsScheduledReminder(id.ToString(), ip);
            await _simplifiedTlsEntity.Handle(reminder);

            A.CallTo(() => _dispatcher.Dispatch(A<SimplifiedTlsTestPending>.That.Matches(_ => _.Id == ip), A<string>._))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task Handle_SimplifiedTlsTestResults_ShouldDispatchMessageForEachHost()
        {
            string ip = "127.0.0.1";
            string hostname1 = "hostname1";
            string hostname2 = "hostname2";
            string hostname3 = "hostname3";

            List<SimplifiedTlsEntityState> entities = new List<SimplifiedTlsEntityState>()
            {
                new SimplifiedTlsEntityState(hostname1, ip),
                new SimplifiedTlsEntityState(hostname2, ip),
                new SimplifiedTlsEntityState(hostname3, ip)
            };

            A.CallTo(() => _hostnameIpAddressDao.FindRelatedEntitiesByIp(ip)).Returns((3, entities));

            NamedAdvisory advisoryMessage = new NamedAdvisory(
                    Guid.NewGuid(),
                    "mailcheck.tls.testName1",
                    MessageType.info,
                    "Some advisory text",
                    "Some advisory markdown"
            );
            SimplifiedTlsConnectionResult connectionResults = new SimplifiedTlsConnectionResult();
            connectionResults.TestName = "Test Name";

            SimplifiedTlsTestResults results = new SimplifiedTlsTestResults(ip);

            results.Certificates = new Dictionary<string, string>() { { "0", "test-certificate-0" } };
            results.AdvisoryMessages = new List<NamedAdvisory>() { advisoryMessage };
            results.SimplifiedTlsConnectionResults = new List<SimplifiedTlsConnectionResult>() { connectionResults };
            await _simplifiedTlsEntity.Handle(results);

            foreach (var entity in entities)
            {
                A.CallTo(() => _dispatcher.Dispatch(
                    A<SimplifiedHostCertificateResult>.That.Matches(
                        r => r.Id == ip &&
                        r.Hostnames.Contains(entity.Hostname) &&
                        r.Certificates == results.Certificates &&
                        r.SimplifiedTlsConnectionResults == results.SimplifiedTlsConnectionResults
                    ), A<string>._
                )).MustHaveHappenedOnceExactly();
            }
        }

        [Test]
        public async Task Handle_SimplifiedTlsTestResults_ShouldNotDispatchWhenIpAddressHasNoAssociatedHostnames()
        {
            string ip = "127.0.0.1";
            A.CallTo(() => _hostnameIpAddressDao.FindEntitiesByIp(ip)).Returns((0, new List<SimplifiedTlsEntityState>()));

            SimplifiedTlsTestResults results = new SimplifiedTlsTestResults(ip);

            results.Certificates = new Dictionary<string, string>();
            results.AdvisoryMessages = new List<NamedAdvisory>();
            results.SimplifiedTlsConnectionResults = new List<SimplifiedTlsConnectionResult>();
            await _simplifiedTlsEntity.Handle(results);

            A.CallTo(() => _dispatcher.Dispatch(A<SimplifiedHostCertificateResult>._, A<string>._)).MustNotHaveHappened();
        }

        [Test]
        public async Task Handle_SimplifiedTlsTestResults_ShouldUpdateStateWithNewTlsAdvisories()
        {
            string hostname = "hostname";
            string ipAddress = "127.0.0.1";

            NamedAdvisory advisoryMessage1 = new NamedAdvisory(Guid.NewGuid(), "mailcheck.tls.testname1", MessageType.info, "TEXT1", "MARKDOWN1");
            List<NamedAdvisory> oldAdvisories = new List<NamedAdvisory>() { advisoryMessage1, advisoryMessage1 };

            NamedAdvisory advisoryMessage2 = new NamedAdvisory(Guid.NewGuid(), "mailcheck.tls.testname2", MessageType.info, "TEXT2", "MARKDOWN2");
            List<NamedAdvisory> newAdvisories = new List<NamedAdvisory>() { advisoryMessage2, advisoryMessage2 };

            DateTime timestamp = new DateTime(2020, 1, 2, 3, 4, 5);
            Dictionary<string, string> certificates = new Dictionary<string, string>();
            SimplifiedTlsTestResults message = new SimplifiedTlsTestResults(ipAddress)
            {
                Timestamp = timestamp,
                AdvisoryMessages = newAdvisories,
                SimplifiedTlsConnectionResults = new List<SimplifiedTlsConnectionResult>(),
                Certificates = certificates
            };

            SimplifiedTlsEntityState hostRecord = new SimplifiedTlsEntityState(hostname, ipAddress);

            SimplifiedTlsEntityState ipRecord = new SimplifiedTlsEntityState("*", ipAddress)
            {
                TlsAdvisories = oldAdvisories
            };

            var findResult = (2, new List<SimplifiedTlsEntityState> { hostRecord, ipRecord });
            A.CallTo(() => _hostnameIpAddressDao.FindRelatedEntitiesByIp(ipAddress)).Returns(findResult);

            List<string> domains = new List<string> { "example.com" };

            Dictionary<string, List<string>> domainsByHostname = new Dictionary<string, List<string>>
            {
                [hostname] = domains
            };

            List<SimplifiedTlsConnectionResult> tlsResults = new List<SimplifiedTlsConnectionResult>();

            A.CallTo(() => _hostnameIpAddressDao.GetDomainsByHostnameForIp(ipAddress)).Returns(domainsByHostname);

            await _simplifiedTlsEntity.Handle(message);

            var expectedHostState = new SimplifiedTlsEntityState
            {
                IpAddress = ipAddress,
                Hostname = "*",
                TlsAdvisories = newAdvisories,
                TlsLastUpdated = timestamp,
                SimplifiedTlsConnectionResults = tlsResults
            };

            var a = JObject.FromObject(expectedHostState);
            A.CallTo(() => _hostnameIpAddressDao.SaveState(A<SimplifiedTlsEntityState>.That.Matches(FuzzyMatches(expectedHostState), "Host entity matches"))).MustHaveHappenedOnceExactly();

            A.CallTo(() => _entityChangedPublisher.Publish(
                hostname,
                A<SimplifiedEmailSecTlsEntityState>.That.Matches(x =>
                    x.Hostname == hostname &&
                    x.TlsAdvisories.Count() == 1 &&
                    x.TlsAdvisories[0].Text == "TEXT2"),
                A<string>._))
            .MustHaveHappenedOnceExactly();

            A.CallTo(() => _tlsChangeNotifier.Notify(
                hostname,
                domains,
                A<IEnumerable<NamedAdvisory>>.That.Matches(x => x.Count() == 1 && x.ToList()[0].Text == "TEXT1"),
                A<IEnumerable<NamedAdvisory>>.That.Matches(x => x.Count() == 1 && x.ToList()[0].Text == "TEXT2"))).MustHaveHappenedOnceExactly();
            A.CallTo(() => _findingsChangedNotifier.Handle(
                hostname,
                domains,
                "tls",
                A<IEnumerable<NamedAdvisory>>.That.Matches(x => x.Count() == 1 && x.ToList()[0].Text == "TEXT1"),
                A<IEnumerable<NamedAdvisory>>.That.Matches(x => x.Count() == 1 && x.ToList()[0].Text == "TEXT2"))).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task Handle_SimplifiedTlsTestResults_ShouldUpdateStateWithNewTlsAdvisoriesWithoutExistingRecord()
        {
            string hostname = "hostname";
            string ipAddress = "127.0.0.1";

            NamedAdvisory newAdvisoryMessage = new NamedAdvisory(Guid.NewGuid(), "mailcheck.tls.testnam1", MessageType.info, "TEXT2", "MARKDOWN2");
            List<NamedAdvisory> newAdvisories = new List<NamedAdvisory> { newAdvisoryMessage };

            DateTime timestamp = new DateTime(2020, 1, 2, 3, 4, 5);
            Dictionary<string, string> certificates = new Dictionary<string, string>();
            SimplifiedTlsTestResults message = new SimplifiedTlsTestResults(ipAddress)
            {
                Timestamp = timestamp,
                AdvisoryMessages = newAdvisories,
                SimplifiedTlsConnectionResults = new List<SimplifiedTlsConnectionResult>(),
                Certificates = certificates
            };

            SimplifiedTlsEntityState hostRecord = new SimplifiedTlsEntityState(hostname, ipAddress);

            var findResult = (1, new List<SimplifiedTlsEntityState> { hostRecord });
            A.CallTo(() => _hostnameIpAddressDao.FindRelatedEntitiesByIp(ipAddress)).Returns(findResult);

            List<string> domains = new List<string> { "example.com" };

            Dictionary<string, List<string>> domainsByHostname = new Dictionary<string, List<string>>
            {
                [hostname] = domains
            };

            List<SimplifiedTlsConnectionResult> tlsResults = new List<SimplifiedTlsConnectionResult>();

            A.CallTo(() => _hostnameIpAddressDao.GetDomainsByHostnameForIp(ipAddress)).Returns(domainsByHostname);

            await _simplifiedTlsEntity.Handle(message);

            var expectedHostState = new SimplifiedTlsEntityState
            {
                IpAddress = ipAddress,
                Hostname = "*",
                TlsAdvisories = newAdvisories,
                TlsLastUpdated = timestamp,
                SimplifiedTlsConnectionResults = tlsResults
            };

            A.CallTo(() => _hostnameIpAddressDao.SaveState(A<SimplifiedTlsEntityState>.That.Matches(FuzzyMatches(expectedHostState), "Host entity matches"))).MustHaveHappenedOnceExactly();

            A.CallTo(() => _entityChangedPublisher.Publish(
                hostname,
                A<SimplifiedEmailSecTlsEntityState>.That.Matches(x =>
                    x.Hostname == hostname &&
                    x.TlsAdvisories.Count() == 1 &&
                    x.TlsAdvisories[0].Text == "TEXT2"),
                A<string>._))
            .MustHaveHappenedOnceExactly();

            A.CallTo(() => _tlsChangeNotifier.Notify(
                hostname,
                domains,
                A<IEnumerable<NamedAdvisory>>.That.Matches(x => !x.Any()),
                A<IEnumerable<NamedAdvisory>>.That.Matches(x => x.ToList()[0].Text == "TEXT2"))).MustHaveHappenedOnceExactly();
            A.CallTo(() => _findingsChangedNotifier.Handle(
                hostname,
                domains,
                "tls",
                A<IEnumerable<NamedAdvisory>>.That.Matches(x => !x.Any()),
                A<IEnumerable<NamedAdvisory>>.That.Matches(x => x.ToList()[0].Text == "TEXT2"))).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task Handle_SimplifiedHostCertificateEvaluated_ShouldUpdateStateWithNewCertAdvisories()
        {
            string hostname = "hostname";
            string ipAddress = "127.0.0.1";

            NamedAdvisory advisoryMessage1 = new NamedAdvisory(Guid.NewGuid(), "mailcheck.tls.testname1", MessageType.info, "OLD HOST", "MARKDOWN");
            List<NamedAdvisory> oldHostAdvisories = new List<NamedAdvisory> { advisoryMessage1, advisoryMessage1 };

            NamedAdvisory advisoryMessage2 = new NamedAdvisory(Guid.NewGuid(), "mailcheck.tls.testname2", MessageType.info, "NEW GLOBAL", "MARKDOWN");
            List<NamedAdvisory> newGlobalAdvisories = new List<NamedAdvisory>() { advisoryMessage2 };

            NamedAdvisory advisoryMessage3 = new NamedAdvisory(Guid.NewGuid(), "mailcheck.tls.testname3", MessageType.info, "NEW HOST", "MARKDOWN");
            List<NamedAdvisory> newHostAdvisories = new List<NamedAdvisory>() { advisoryMessage3, advisoryMessage3 };

            NamedAdvisory advisoryMessage4 = new NamedAdvisory(Guid.NewGuid(), "mailcheck.tls.testname4", MessageType.info, "OLD GLOBAL", "MARKDOWN");
            List<NamedAdvisory> oldGlobalAdvisories = new List<NamedAdvisory>() { advisoryMessage4 };

            DateTime timestamp = new DateTime(2020, 1, 2, 3, 4, 5);
            SimplifiedHostCertificateEvaluated message = new SimplifiedHostCertificateEvaluated(ipAddress)
            {
                Timestamp = timestamp,
                Hostnames = new List<string> { hostname },
                CertificateAdvisoryMessages = newGlobalAdvisories,
                HostSpecificCertificateAdvisoryMessages = new Dictionary<string, List<NamedAdvisory>>
                {
                    [hostname] = newHostAdvisories
                },
                Certificates = new Dictionary<string, string>()
            };

            SimplifiedTlsEntityState state = new SimplifiedTlsEntityState(hostname, ipAddress)
            {
                CertAdvisories = oldHostAdvisories
            };

            SimplifiedTlsEntityState globalState = new SimplifiedTlsEntityState("*", ipAddress)
            {
                CertAdvisories = oldGlobalAdvisories
            };

            A.CallTo(() => _hostnameIpAddressDao.FindRelatedEntitiesByIp(ipAddress)).Returns((2, new List<SimplifiedTlsEntityState> { state, globalState }));

            List<string> domains = new List<string> { "example.com" };

            Dictionary<string, List<string>> domainsByHostname = new Dictionary<string, List<string>>
            {
                [hostname] = domains
            };

            A.CallTo(() => _hostnameIpAddressDao.GetDomainsByHostnameForIp(ipAddress)).Returns(domainsByHostname);

            await _simplifiedTlsEntity.Handle(message);

            var expectedIpState = new SimplifiedTlsEntityState
            {
                IpAddress = ipAddress,
                Hostname = "*",
                CertAdvisories = newGlobalAdvisories,
                CertsLastUpdated = timestamp,
                Certificates = message.Certificates
            };

            var a = JObject.FromObject(expectedIpState);
            A.CallTo(() => _hostnameIpAddressDao.SaveState(A<SimplifiedTlsEntityState>.That.Matches(FuzzyMatches(expectedIpState), "IP entity matches"))).MustHaveHappenedOnceExactly();

            var expectedHostState = new SimplifiedTlsEntityState
            {
                IpAddress = ipAddress,
                Hostname = hostname,
                CertAdvisories = newHostAdvisories,
                CertsLastUpdated = timestamp,
            };

            A.CallTo(() => _hostnameIpAddressDao.SaveState(A<SimplifiedTlsEntityState>.That.Matches(FuzzyMatches(expectedHostState), "Host entity matches"))).MustHaveHappenedOnceExactly();

            A.CallTo(() => _entityChangedPublisher.Publish(
                hostname,
                A<SimplifiedEmailSecTlsEntityState>.That.Matches(x =>
                    x.Hostname == hostname &&
                    x.CertAdvisories.Count() == 2 &&
                    x.CertAdvisories[0].Text == "NEW HOST" &&
                    x.CertAdvisories[1].Text == "NEW GLOBAL"),
                A<string>._))
            .MustHaveHappenedOnceExactly();

            A.CallTo(() => _certChangeNotifier.Notify(
                hostname,
                domains,
                A<IEnumerable<NamedAdvisory>>.That.Matches(x => x.Count() == 2 && x.ToList()[0].Text == "OLD HOST" && x.ToList()[1].Text == "OLD GLOBAL"),
                A<IEnumerable<NamedAdvisory>>.That.Matches(x => x.Count() == 2 && x.ToList()[0].Text == "NEW HOST" && x.ToList()[1].Text == "NEW GLOBAL")
            )).MustHaveHappenedOnceExactly();

            A.CallTo(() => _findingsChangedNotifier.Handle(
                hostname,
                domains,
                "tls-certificates",
                A<IEnumerable<NamedAdvisory>>.That.Matches(x => x.Count() == 2 && x.ToList()[0].Text == "OLD HOST" && x.ToList()[1].Text == "OLD GLOBAL"),
                A<IEnumerable<NamedAdvisory>>.That.Matches(x => x.Count() == 2 && x.ToList()[0].Text == "NEW HOST" && x.ToList()[1].Text == "NEW GLOBAL")
            )).MustHaveHappenedOnceExactly();

            A.CallTo(() => _dispatcher.Dispatch(
                A<ReminderSuccessful>.That.Matches(x => x.ResourceId == ipAddress),
                A<string>.Ignored
            )).MustHaveHappenedOnceExactly();

            A.CallTo(() => _domainStatusPublisher.CalculateAndPublishDomainStatuses(ipAddress)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task Handle_SimplifiedHostCertificateEvaluated_ShouldShallowCloneAllFields()
        {
            string hostname = "hostname";
            string ipAddress = "127.0.0.1";

            NamedAdvisory advisoryMessage1 = new NamedAdvisory(Guid.NewGuid(), "mailcheck.tls.testname1", MessageType.info, "OLD HOST", "MARKDOWN");
            List<NamedAdvisory> oldHostAdvisories = new List<NamedAdvisory> { advisoryMessage1, advisoryMessage1 };

            NamedAdvisory advisoryMessage2 = new NamedAdvisory(Guid.NewGuid(), "mailcheck.tls.testname2", MessageType.info, "NEW GLOBAL", "MARKDOWN");
            List<NamedAdvisory> newGlobalAdvisories = new List<NamedAdvisory>() { advisoryMessage2 };

            NamedAdvisory advisoryMessage3 = new NamedAdvisory(Guid.NewGuid(), "mailcheck.tls.testname3", MessageType.info, "NEW HOST", "MARKDOWN");
            List<NamedAdvisory> newHostAdvisories = new List<NamedAdvisory>() { advisoryMessage3, advisoryMessage3 };

            NamedAdvisory advisoryMessage4 = new NamedAdvisory(Guid.NewGuid(), "mailcheck.tls.testname4", MessageType.info, "OLD GLOBAL", "MARKDOWN");
            List<NamedAdvisory> oldGlobalAdvisories = new List<NamedAdvisory>() { advisoryMessage4 };

            DateTime timestamp = new DateTime(2020, 1, 2, 3, 4, 5);
            SimplifiedHostCertificateEvaluated message = new SimplifiedHostCertificateEvaluated(ipAddress)
            {
                Timestamp = timestamp,
                Hostnames = new List<string> { hostname },
                CertificateAdvisoryMessages = newGlobalAdvisories,
                HostSpecificCertificateAdvisoryMessages = new Dictionary<string, List<NamedAdvisory>>
                {
                    [hostname] = newHostAdvisories
                },
                Certificates = new Dictionary<string, string>()
            };

            List<SimplifiedTlsConnectionResult> simplifiedTlsConnectionResults = new List<SimplifiedTlsConnectionResult>
            {
                new SimplifiedTlsConnectionResult(),
                new SimplifiedTlsConnectionResult(),
            };

            SimplifiedTlsEntityState state = new SimplifiedTlsEntityState(hostname, ipAddress)
            {
                CertAdvisories = oldHostAdvisories,
                SimplifiedTlsConnectionResults = simplifiedTlsConnectionResults
            };

            SimplifiedTlsEntityState globalState = new SimplifiedTlsEntityState("*", ipAddress)
            {
                CertAdvisories = oldGlobalAdvisories,
                SimplifiedTlsConnectionResults = simplifiedTlsConnectionResults
            };

            A.CallTo(() => _hostnameIpAddressDao.FindRelatedEntitiesByIp(ipAddress)).Returns((2, new List<SimplifiedTlsEntityState> { state, globalState }));

            List<string> domains = new List<string> { "example.com" };

            Dictionary<string, List<string>> domainsByHostname = new Dictionary<string, List<string>>
            {
                [hostname] = domains
            };

            A.CallTo(() => _hostnameIpAddressDao.GetDomainsByHostnameForIp(ipAddress)).Returns(domainsByHostname);

            await _simplifiedTlsEntity.Handle(message);

            var expectedIpState = new SimplifiedTlsEntityState
            {
                IpAddress = ipAddress,
                Hostname = "*",
                CertAdvisories = newGlobalAdvisories,
                CertsLastUpdated = timestamp,
                SimplifiedTlsConnectionResults = simplifiedTlsConnectionResults,
                Certificates = message.Certificates
            };

            var a = JObject.FromObject(expectedIpState);
            A.CallTo(() => _hostnameIpAddressDao.SaveState(A<SimplifiedTlsEntityState>.That.Matches(FuzzyMatches(expectedIpState), "IP entity matches"))).MustHaveHappenedOnceExactly();

            var expectedHostState = new SimplifiedTlsEntityState
            {
                IpAddress = ipAddress,
                Hostname = hostname,
                CertAdvisories = newHostAdvisories,
                CertsLastUpdated = timestamp,
                SimplifiedTlsConnectionResults = simplifiedTlsConnectionResults
            };

            A.CallTo(() => _hostnameIpAddressDao.SaveState(A<SimplifiedTlsEntityState>.That.Matches(FuzzyMatches(expectedHostState), "Host entity matches"))).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task ShouldSaveRootCertificatesInThumbprint()
        {
            SimplifiedHostCertificateEvaluated input = new SimplifiedHostCertificateEvaluated("ip")
            {
                RootCertificateThumbprint = "TEST ROOT CERT",
                CertificateAdvisoryMessages = new List<NamedAdvisory>(),
                Hostnames = new List<string>()
            };

            SimplifiedTlsConnectionResult result = new SimplifiedTlsConnectionResult()
            {
                CertificateThumbprints = new string[] { "A CERTIFICATE" }
            };
            SimplifiedTlsEntityState state = new SimplifiedTlsEntityState(){
                Hostname = "*",
                IpAddress = "ip",
                SimplifiedTlsConnectionResults = new List<SimplifiedTlsConnectionResult>{ result }
            };
            A.CallTo(() => _hostnameIpAddressDao.FindRelatedEntitiesByIp("ip")).Returns((1, new List<SimplifiedTlsEntityState>() { state }));

            await _simplifiedTlsEntity.Handle(input);

            A.CallTo(() => _hostnameIpAddressDao.SaveState(A<SimplifiedTlsEntityState>.That.Matches(
                    state => state.SimplifiedTlsConnectionResults[0].CertificateThumbprints.Any(thumbprint => thumbprint == input.RootCertificateThumbprint))
                )).MustHaveHappened();
        }

        public static Func<T, bool> FuzzyMatches<T>(T example)
        {
            return actual =>
            {
                return JToken.DeepEquals(JObject.FromObject(example), JObject.FromObject(actual));
            };
        }
    }
}
