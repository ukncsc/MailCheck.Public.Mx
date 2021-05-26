using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Mx.Contracts.Entity;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.TlsEvaluator;
using MailCheck.Mx.TlsEntity.Config;
using MailCheck.Mx.TlsEntity.Entity.Notifications;
using MailCheck.Mx.TlsEntity.Entity.Notifiers;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace MailCheck.Mx.TlsEntity.Test.Entity.Notifiers
{
    [TestFixture]
    public class AdvisoryChangedNotifiersTests
    {
        private IMessagePublisher _messagePublisher;
        private ITlsEntityConfig _tlsEntityConfig;
        private ILogger<AdvisoryChangedNotifier> _log;
        private AdvisoryChangedNotifier _advisoryChangedNotifier;

        [SetUp]
        public void SetUp()
        {
            _messagePublisher = A.Fake<IMessagePublisher>();
            _tlsEntityConfig = A.Fake<ITlsEntityConfig>();
            _log = A.Fake<ILogger<AdvisoryChangedNotifier>>();

            _advisoryChangedNotifier = new AdvisoryChangedNotifier(
                _messagePublisher,
                _tlsEntityConfig,
                _log);
        }

        [TestCaseSource(nameof(ExerciseEqualityComparersTestPermutations))]
        public Task ExerciseEqualityComparers(AdvisoryChangedNotifierTestCase testCase)
        {
            TlsEntityState state = new TlsEntityState("host.mailserv.com")
            {
                TlsRecords = testCase.CurrentTlsRecords,
                CertificateResults = new CertificateResults(null, testCase.CurrentCertErrors)
            };

            TlsResultsEvaluated message = new TlsResultsEvaluated(
                "host.mailserv.com",
                false,
                testCase.NewTlsRecords,
                new CertificateResults(null, testCase.NewCertErrors)
            );

            _advisoryChangedNotifier.Handle(state, message, testCase.Domains);

            if (testCase.ExpectedConfigAdded > 0)
            {
                A.CallTo(() => _messagePublisher.Publish(A<TlsAdvisoryAdded>.That.Matches(x => x.Host == "host.mailserv.com" && x.Messages.Count == testCase.ExpectedConfigAdded), A<string>._))
                 .MustHaveHappened(testCase.Domains.Count, Times.Exactly);
            }
            else
            {
                A.CallTo(() => _messagePublisher.Publish(A<TlsAdvisoryAdded>.That.Matches(x => x.Host == "host.mailserv.com" && x.Messages.Count == testCase.ExpectedConfigAdded), A<string>._))
                 .MustNotHaveHappened();
            }

            if (testCase.ExpectedConfigSustained > 0)
            {
                A.CallTo(() => _messagePublisher.Publish(A<TlsAdvisorySustained>.That.Matches(x => x.Host == "host.mailserv.com" && x.Messages.Count == testCase.ExpectedConfigSustained), A<string>._))
                 .MustHaveHappened(testCase.Domains.Count, Times.Exactly);
            }
            else
            {
                A.CallTo(() => _messagePublisher.Publish(A<TlsAdvisorySustained>.That.Matches(x => x.Host == "host.mailserv.com" && x.Messages.Count == testCase.ExpectedConfigSustained), A<string>._))
                 .MustNotHaveHappened();
            }

            if (testCase.ExpectedConfigRemoved > 0)
            {
                A.CallTo(() => _messagePublisher.Publish(A<TlsAdvisoryRemoved>.That.Matches(x => x.Host == "host.mailserv.com" && x.Messages.Count == testCase.ExpectedConfigRemoved), A<string>._))
                 .MustHaveHappened(testCase.Domains.Count, Times.Exactly);
            }
            else
            {
                A.CallTo(() => _messagePublisher.Publish(A<TlsAdvisoryRemoved>.That.Matches(x => x.Host == "host.mailserv.com" && x.Messages.Count == testCase.ExpectedConfigRemoved), A<string>._))
                 .MustNotHaveHappened();
            }

            if (testCase.ExpectedCertAdded > 0)
            {
                A.CallTo(() => _messagePublisher.Publish(A<TlsCertAdvisoryAdded>.That.Matches(x => x.Host == "host.mailserv.com" && x.Messages.Count == testCase.ExpectedCertAdded), A<string>._))
                 .MustHaveHappened(testCase.Domains.Count, Times.Exactly);
            }
            else
            {
                A.CallTo(() => _messagePublisher.Publish(A<TlsCertAdvisoryAdded>.That.Matches(x => x.Host == "host.mailserv.com" && x.Messages.Count == testCase.ExpectedCertAdded), A<string>._))
                 .MustNotHaveHappened();
            }

            if (testCase.ExpectedCertSustained > 0)
            {
                A.CallTo(() => _messagePublisher.Publish(A<TlsCertAdvisorySustained>.That.Matches(x => x.Host == "host.mailserv.com" && x.Messages.Count == testCase.ExpectedCertSustained), A<string>._))
                 .MustHaveHappened(testCase.Domains.Count, Times.Exactly);
            }
            else
            {
                A.CallTo(() => _messagePublisher.Publish(A<TlsCertAdvisorySustained>.That.Matches(x => x.Host == "host.mailserv.com" && x.Messages.Count == testCase.ExpectedCertSustained), A<string>._))
                 .MustNotHaveHappened();
            }

            if (testCase.ExpectedCertRemoved > 0)
            {
                A.CallTo(() => _messagePublisher.Publish(A<TlsCertAdvisoryRemoved>.That.Matches(x => x.Host == "host.mailserv.com" && x.Messages.Count == testCase.ExpectedCertRemoved), A<string>._))
                 .MustHaveHappened(testCase.Domains.Count, Times.Exactly);
            }
            else
            {
                A.CallTo(() => _messagePublisher.Publish(A<TlsCertAdvisoryRemoved>.That.Matches(x => x.Host == "host.mailserv.com" && x.Messages.Count == testCase.ExpectedCertRemoved), A<string>._))
                 .MustNotHaveHappened();
            }

            return Task.CompletedTask;
        }

        [Test]
        public Task ExceriseNullState()
        {
            TlsEntityState state = new TlsEntityState("host.mailserv.com");

            List<TlsRecord> warningRecords = Enumerable.Range(0, 15)
                .Select(i => new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.WARNING, $"test warning result {i}"))
                .Select(result => new TlsRecord(result))
                .ToList();


            TlsRecords AllWarningTlsRecords = new TlsRecords(
                warningRecords[0],
                warningRecords[1],
                warningRecords[2],
                warningRecords[3],
                warningRecords[4],
                warningRecords[5],
                warningRecords[6],
                warningRecords[7],
                warningRecords[8],
                warningRecords[9],
                warningRecords[10],
                warningRecords[11],
                warningRecords[12],
                warningRecords[13],
                warningRecords[14]
            );

            Error errorError = new Error(ErrorType.Error, "test error error");
            Error warningError = new Error(ErrorType.Warning, "test warning error");
            Error incoError = new Error(ErrorType.Inconclusive, "test inconclusive error");

            TlsResultsEvaluated message = new TlsResultsEvaluated(
                "host.mailserv.com",
                false,
                AllWarningTlsRecords,
                new CertificateResults(null, new List<Error> { errorError, warningError, incoError })
            );

            _advisoryChangedNotifier.Handle(state, message, new List<string> { "testdomain.gov.uk" });

            A.CallTo(() => _messagePublisher.Publish(A<TlsAdvisoryAdded>.That.Matches(x => x.Host == "host.mailserv.com" && x.Messages.Count == 15), A<string>._))
                .MustHaveHappened(1, Times.Exactly);

            A.CallTo(() => _messagePublisher.Publish(A<TlsAdvisorySustained>._, A<string>._))
                .MustNotHaveHappened();

            A.CallTo(() => _messagePublisher.Publish(A<TlsAdvisoryRemoved>._, A<string>._))
                .MustNotHaveHappened();

            A.CallTo(() => _messagePublisher.Publish(A<TlsCertAdvisoryAdded>.That.Matches(x => x.Host == "host.mailserv.com" && x.Messages.Count == 3), A<string>._))
                .MustHaveHappened(1, Times.Exactly);

            A.CallTo(() => _messagePublisher.Publish(A<TlsCertAdvisorySustained>._, A<string>._))
                .MustNotHaveHappened();

            A.CallTo(() => _messagePublisher.Publish(A<TlsCertAdvisoryRemoved>._, A<string>._))
                .MustNotHaveHappened();

            return Task.CompletedTask;

        }

        private static IEnumerable<AdvisoryChangedNotifierTestCase> ExerciseEqualityComparersTestPermutations()
        {
            List<TlsRecord> failRecords = Enumerable.Range(0, 15)
                .Select(i => new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.FAIL, $"test fail result {i}"))
                .Select(result => new TlsRecord(result))
                .ToList();

            List<TlsRecord> warningRecords = Enumerable.Range(0, 15)
                .Select(i => new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.WARNING, $"test warning result {i}"))
                .Select(result => new TlsRecord(result))
                .ToList();

            List<TlsRecord> infoRecords = Enumerable.Range(0, 15)
                .Select(i => new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.INFORMATIONAL, $"test info result {i}"))
                .Select(result => new TlsRecord(result))
                .ToList();

            List<TlsRecord> passRecords = Enumerable.Range(0, 15)
                .Select(i => new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS, $"test pass result {i}"))
                .Select(result => new TlsRecord(result))
                .ToList();

            List<TlsRecord> nullRecords = Enumerable.Range(0, 15)
                .Select(i => new TlsEvaluatedResult(Guid.NewGuid(), null, $"test null result {i}"))
                .Select(result => new TlsRecord(result))
                .ToList();

            Error errorError = new Error(ErrorType.Error, "test error error");
            Error warningError = new Error(ErrorType.Warning, "test warning error");
            Error incoError = new Error(ErrorType.Inconclusive, "test inconclusive error");

            TlsRecords AllFailTlsRecords = new TlsRecords(
                failRecords[0],
                failRecords[1],
                failRecords[2],
                failRecords[3],
                failRecords[4],
                failRecords[5],
                failRecords[6],
                failRecords[7],
                failRecords[8],
                failRecords[9],
                failRecords[10],
                failRecords[11],
                failRecords[12],
                failRecords[13],
                failRecords[14]
            );

            TlsRecords AllWarningTlsRecords = new TlsRecords(
                warningRecords[0],
                warningRecords[1],
                warningRecords[2],
                warningRecords[3],
                warningRecords[4],
                warningRecords[5],
                warningRecords[6],
                warningRecords[7],
                warningRecords[8],
                warningRecords[9],
                warningRecords[10],
                warningRecords[11],
                warningRecords[12],
                warningRecords[13],
                warningRecords[14]
            );

            TlsRecords SomeInfoSomeFailTlsRecords = new TlsRecords(
                failRecords[0],
                infoRecords[1],
                failRecords[2],
                infoRecords[3],
                failRecords[4],
                infoRecords[5],
                failRecords[6],
                infoRecords[7],
                failRecords[8],
                infoRecords[9],
                failRecords[10],
                infoRecords[11],
                failRecords[12],
                infoRecords[13],
                failRecords[14]
            );

            TlsRecords NullRecords = new TlsRecords(
                nullRecords[0],
                nullRecords[1],
                nullRecords[2],
                nullRecords[3],
                nullRecords[4],
                nullRecords[5],
                nullRecords[6],
                nullRecords[7],
                nullRecords[8],
                nullRecords[9],
                nullRecords[10],
                nullRecords[11],
                nullRecords[12],
                nullRecords[13],
                nullRecords[14]
            );

            TlsRecords PassRecords = new TlsRecords(
                passRecords[0],
                passRecords[1],
                passRecords[2],
                passRecords[3],
                passRecords[4],
                passRecords[5],
                passRecords[6],
                passRecords[7],
                passRecords[8],
                passRecords[9],
                passRecords[10],
                passRecords[11],
                passRecords[12],
                passRecords[13],
                passRecords[14]
            );

            AdvisoryChangedNotifierTestCase test1 = new AdvisoryChangedNotifierTestCase
            {
                CurrentTlsRecords = AllFailTlsRecords,
                CurrentCertErrors = new List<Error>(),
                NewTlsRecords = AllWarningTlsRecords,
                NewCertErrors = new List<Error> { errorError, warningError },
                Domains = new List<string> { "ncsc.gov.uk" },
                ExpectedConfigAdded = 15,
                ExpectedConfigSustained = 0,
                ExpectedConfigRemoved = 15,
                ExpectedCertAdded = 2,
                ExpectedCertSustained = 0,
                ExpectedCertRemoved = 0,
                Description = "all Fail -> all Warning, no cert errors -> two errors, 1 domain"
            };

            AdvisoryChangedNotifierTestCase test2 = new AdvisoryChangedNotifierTestCase
            {
                CurrentTlsRecords = AllFailTlsRecords,
                CurrentCertErrors = new List<Error>(),
                NewTlsRecords = AllWarningTlsRecords,
                NewCertErrors = new List<Error> { errorError, warningError },
                Domains = new List<string> { "ncsc.gov.uk", "defra.gov.uk", "beis.gov.uk" },
                ExpectedConfigAdded = 15,
                ExpectedConfigSustained = 0,
                ExpectedConfigRemoved = 15,
                ExpectedCertAdded = 2,
                ExpectedCertSustained = 0,
                ExpectedCertRemoved = 0,
                Description = "all Fail -> all Warning, no cert errors -> two errors, 3 domains"
            };

            AdvisoryChangedNotifierTestCase test3 = new AdvisoryChangedNotifierTestCase
            {
                CurrentTlsRecords = AllWarningTlsRecords,
                CurrentCertErrors = new List<Error>(),
                NewTlsRecords = AllFailTlsRecords,
                NewCertErrors = new List<Error> { errorError, warningError },
                Domains = new List<string> { "ncsc.gov.uk", "defra.gov.uk", "beis.gov.uk" },
                ExpectedConfigAdded = 15,
                ExpectedConfigSustained = 0,
                ExpectedConfigRemoved = 15,
                ExpectedCertAdded = 2,
                ExpectedCertSustained = 0,
                ExpectedCertRemoved = 0,
                Description = "all Warning -> all Fail, no cert errors -> two errors, 3 domains"
            };

            AdvisoryChangedNotifierTestCase test4 = new AdvisoryChangedNotifierTestCase
            {
                CurrentTlsRecords = AllWarningTlsRecords,
                CurrentCertErrors = new List<Error> { errorError, warningError },
                NewTlsRecords = AllFailTlsRecords,
                NewCertErrors = new List<Error>(),
                Domains = new List<string> { "ncsc.gov.uk", "defra.gov.uk", "beis.gov.uk" },
                ExpectedConfigAdded = 15,
                ExpectedConfigSustained = 0,
                ExpectedConfigRemoved = 15,
                ExpectedCertAdded = 0,
                ExpectedCertSustained = 0,
                ExpectedCertRemoved = 2,
                Description = "all Warning -> all Fail, two cert errors -> no errors, 3 domains"
            };

            AdvisoryChangedNotifierTestCase test5 = new AdvisoryChangedNotifierTestCase
            {
                CurrentTlsRecords = AllFailTlsRecords,
                CurrentCertErrors = new List<Error> { errorError, warningError },
                NewTlsRecords = AllFailTlsRecords,
                NewCertErrors = new List<Error>(),
                Domains = new List<string> { "ncsc.gov.uk", "defra.gov.uk", "beis.gov.uk" },
                ExpectedConfigAdded = 0,
                ExpectedConfigSustained = 15,
                ExpectedConfigRemoved = 0,
                ExpectedCertAdded = 0,
                ExpectedCertSustained = 0,
                ExpectedCertRemoved = 2,
                Description = "all Fail -> all Fail, two cert errors -> no errors, 3 domains"
            };

            AdvisoryChangedNotifierTestCase test6 = new AdvisoryChangedNotifierTestCase
            {
                CurrentTlsRecords = AllFailTlsRecords,
                CurrentCertErrors = new List<Error> { errorError, warningError },
                NewTlsRecords = AllFailTlsRecords,
                NewCertErrors = new List<Error> { errorError, warningError },
                Domains = new List<string> { "ncsc.gov.uk", "defra.gov.uk", "beis.gov.uk" },
                ExpectedConfigAdded = 0,
                ExpectedConfigSustained = 15,
                ExpectedConfigRemoved = 0,
                ExpectedCertAdded = 0,
                ExpectedCertSustained = 2,
                ExpectedCertRemoved = 0,
                Description = "all Fail -> all Fail, 2 cert errors -> 2 errors, 3 domains"
            };

            AdvisoryChangedNotifierTestCase test7 = new AdvisoryChangedNotifierTestCase
            {
                CurrentTlsRecords = new TlsRecords(null),
                CurrentCertErrors = new List<Error> { errorError, warningError },
                NewTlsRecords = SomeInfoSomeFailTlsRecords,
                NewCertErrors = new List<Error> { errorError, warningError },
                Domains = new List<string> { "ncsc.gov.uk", "defra.gov.uk", "beis.gov.uk" },
                ExpectedConfigAdded = 15,
                ExpectedConfigSustained = 0,
                ExpectedConfigRemoved = 0,
                ExpectedCertAdded = 0,
                ExpectedCertSustained = 2,
                ExpectedCertRemoved = 0,
                Description = "null -> some Info some Fail, 2 cert errors -> 2 errors, 3 domains"
            };

            AdvisoryChangedNotifierTestCase test8 = new AdvisoryChangedNotifierTestCase
            {
                CurrentTlsRecords = SomeInfoSomeFailTlsRecords,
                CurrentCertErrors = new List<Error> { errorError, warningError },
                NewTlsRecords = new TlsRecords(null),
                NewCertErrors = new List<Error> { errorError, warningError },
                Domains = new List<string> { "ncsc.gov.uk", "defra.gov.uk", "beis.gov.uk" },
                ExpectedConfigAdded = 0,
                ExpectedConfigSustained = 0,
                ExpectedConfigRemoved = 15,
                ExpectedCertAdded = 0,
                ExpectedCertSustained = 2,
                ExpectedCertRemoved = 0,
                Description = "some Info some Fail -> null, 2 cert errors -> 2 errors, 3 domains"
            };

            AdvisoryChangedNotifierTestCase test9 = new AdvisoryChangedNotifierTestCase
            {
                CurrentTlsRecords = SomeInfoSomeFailTlsRecords,
                CurrentCertErrors = new List<Error> { errorError, warningError },
                NewTlsRecords = new TlsRecords(null),
                NewCertErrors = new List<Error> { errorError, warningError },
                Domains = new List<string>(),
                ExpectedConfigAdded = 0,
                ExpectedConfigSustained = 0,
                ExpectedConfigRemoved = 15,
                ExpectedCertAdded = 0,
                ExpectedCertSustained = 2,
                ExpectedCertRemoved = 0,
                Description = "some Info some Fail -> null, 2 cert errors -> 2 errors, 0 domains"
            };

            AdvisoryChangedNotifierTestCase test10 = new AdvisoryChangedNotifierTestCase
            {
                CurrentTlsRecords = SomeInfoSomeFailTlsRecords,
                CurrentCertErrors = new List<Error> { errorError, warningError },
                NewTlsRecords = AllFailTlsRecords,
                NewCertErrors = new List<Error> { errorError, warningError },
                Domains = new List<string> { "ncsc.gov.uk", "defra.gov.uk", "beis.gov.uk" },
                ExpectedConfigAdded = 7,
                ExpectedConfigSustained = 8,
                ExpectedConfigRemoved = 7,
                ExpectedCertAdded = 0,
                ExpectedCertSustained = 2,
                ExpectedCertRemoved = 0,
                Description = "some Info some Fail -> all Fail, 2 cert errors -> 2 errors, 3 domains"
            };

            AdvisoryChangedNotifierTestCase test11 = new AdvisoryChangedNotifierTestCase
            {
                CurrentTlsRecords = NullRecords,
                CurrentCertErrors = new List<Error> { errorError, warningError },
                NewTlsRecords = AllFailTlsRecords,
                NewCertErrors = new List<Error> { errorError, warningError },
                Domains = new List<string> { "ncsc.gov.uk", "defra.gov.uk", "beis.gov.uk" },
                ExpectedConfigAdded = 15,
                ExpectedConfigSustained = 0,
                ExpectedConfigRemoved = 0,
                ExpectedCertAdded = 0,
                ExpectedCertSustained = 2,
                ExpectedCertRemoved = 0,
                Description = "all Null -> all Fail, 2 cert errors -> 2 errors, 3 domains"
            };

            AdvisoryChangedNotifierTestCase test12 = new AdvisoryChangedNotifierTestCase
            {
                CurrentTlsRecords = PassRecords,
                CurrentCertErrors = new List<Error> { errorError, warningError },
                NewTlsRecords = AllFailTlsRecords,
                NewCertErrors = new List<Error> { errorError, warningError },
                Domains = new List<string> { "ncsc.gov.uk", "defra.gov.uk", "beis.gov.uk" },
                ExpectedConfigAdded = 15,
                ExpectedConfigSustained = 0,
                ExpectedConfigRemoved = 0,
                ExpectedCertAdded = 0,
                ExpectedCertSustained = 2,
                ExpectedCertRemoved = 0,
                Description = "all Pass -> all Fail, 2 cert errors -> 2 errors, 3 domains"
            };

            yield return test1;
            yield return test2;
            yield return test3;
            yield return test4;
            yield return test5;
            yield return test6;
            yield return test7;
            yield return test8;
            yield return test9;
            yield return test10;
            yield return test11;
            yield return test12;
        }
    }
}
