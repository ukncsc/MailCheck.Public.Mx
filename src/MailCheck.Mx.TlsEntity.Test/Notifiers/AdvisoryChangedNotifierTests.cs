using System;
using System.Collections.Generic;
using System.Linq;
using FakeItEasy;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Mx.Contracts.Entity;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.TlsEvaluator;
using MailCheck.Mx.TlsEntity.Config;
using MailCheck.Mx.TlsEntity.Entity.Notifications;
using MailCheck.Mx.TlsEntity.Entity.Notifiers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NUnit.Framework;
using Message = MailCheck.Common.Messaging.Abstractions.Message;

namespace MailCheck.Mx.TlsEntity.Test.Notifiers
{
    [TestFixture]
    public class AdvisoryChangedNotifierTests
    {
        private IMessageDispatcher _messageDispatcher;
        private ITlsEntityConfig _tlsEntityConfig;
        private IEqualityComparer<TlsEvaluatedResult> _messageEqualityComparer;
        private AdvisoryChangedNotifier _advisoryChangedNotifier;
        private ILogger<AdvisoryChangedNotifier> _logger;

        [SetUp]
        public void SetUp()
        {
            _messageDispatcher = A.Fake<IMessageDispatcher>();
            _tlsEntityConfig = A.Fake<ITlsEntityConfig>();
            _messageEqualityComparer = new MessageEqualityComparer();
            _logger = A.Fake<ILogger<AdvisoryChangedNotifier>>();
            _advisoryChangedNotifier =
                new AdvisoryChangedNotifier(_messageDispatcher, _tlsEntityConfig, _messageEqualityComparer, _logger);
        }

        [Test]
        public void DoesNotNotifyWhenMessageAreSameIdButDifferentMessage()
        {
            Guid messageId = Guid.NewGuid();

            string errorMessage = "An error has occured";
            string errorMessage2 = "An error has occured 2";

            TlsRecords existingTlsRecords =
                CreateTlsRecords(
                    new TlsRecord(new TlsEvaluatedResult(messageId, EvaluatorResult.FAIL, errorMessage)));

            TlsRecords newTlsRecords =
                CreateTlsRecords(
                    new TlsRecord(new TlsEvaluatedResult(messageId, EvaluatorResult.FAIL, errorMessage2)));

            TlsEntityState existingState = CreateEntityStateWithMessages(existingTlsRecords);

            _advisoryChangedNotifier.Handle(existingState, CreateTlsResultsEvaluatedWithResults(newTlsRecords), new List<string> { "test.gov.uk" });

            A.CallTo(() => _messageDispatcher.Dispatch(A<TlsAdvisoryAdded>._, A<string>._, A<string>._)).MustNotHaveHappened();
            A.CallTo(() => _messageDispatcher.Dispatch(A<TlsAdvisoryRemoved>._, A<string>._, A<string>._)).MustNotHaveHappened();
        }

        [Test]
        public void DoesNotNotifyWhenNoChanges_DataInRecords()
        {
            Guid messageId = Guid.NewGuid();

            string errorMessage = "An error has occured";

            TlsRecords existingTlsRecords =
                CreateTlsRecords(
                    new TlsRecord(new TlsEvaluatedResult(messageId, EvaluatorResult.FAIL, errorMessage)));

            TlsEntityState existingState = CreateEntityStateWithMessages(existingTlsRecords);

            TlsRecords newTlsRecords =
                CreateTlsRecords(
                    new TlsRecord(new TlsEvaluatedResult(messageId, EvaluatorResult.FAIL, errorMessage)));

            _advisoryChangedNotifier.Handle(existingState, CreateTlsResultsEvaluatedWithResults(newTlsRecords), new List<string> { "test.gov.uk" });

            A.CallTo(() => _messageDispatcher.Dispatch(A<Message>._, A<string>._, A<string>._)).MustNotHaveHappened();
        }

        [Test]
        public void NotifiesWhenMessageAdded_DataInRecords()
        {
            Guid messageId = Guid.NewGuid();
            Guid messageId2 = Guid.NewGuid();

            string errorMessage = "An error has occured";
            string errorMessage2 = "An error has occured 2";

            TlsRecords existingTlsRecords =
                CreateTlsRecords(
                    new TlsRecord(new TlsEvaluatedResult(messageId, EvaluatorResult.FAIL, errorMessage)));

            TlsRecords newTlsRecords =
                CreateTlsRecords(
                    new TlsRecord(new TlsEvaluatedResult(messageId, EvaluatorResult.FAIL, errorMessage)),
                    new TlsRecord(new TlsEvaluatedResult(messageId2, EvaluatorResult.FAIL, errorMessage2)));

            TlsEntityState existingState = CreateEntityStateWithMessages(existingTlsRecords);

            _advisoryChangedNotifier.Handle(existingState, CreateTlsResultsEvaluatedWithResults(newTlsRecords), new List<string> { "test.gov.uk" });

            A.CallTo(() => _messageDispatcher.Dispatch(A<TlsAdvisoryAdded>._, A<string>._))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() =>

                _messageDispatcher.Dispatch(
                    A<TlsAdvisoryAdded>.That.Matches(x => x.Messages.First().Text == errorMessage2), A<string>._)).MustHaveHappenedOnceExactly();

            A.CallTo(() => _messageDispatcher.Dispatch(A<TlsAdvisoryRemoved>._, A<string>._))
                .MustNotHaveHappened();
        }

        [Test]
        public void NotifiesWhenMessageRemoved_DataInRecords()
        {
            Guid messageId = Guid.NewGuid();

            string errorMessage = "An error has occured";

            TlsRecords existingTlsRecords =
                CreateTlsRecords(
                    new TlsRecord(new TlsEvaluatedResult(messageId, EvaluatorResult.FAIL, errorMessage)));

            TlsEntityState existingState = CreateEntityStateWithMessages(existingTlsRecords);

            TlsRecords newTlsRecords =
                CreateTlsRecords(
                    new TlsRecord(new TlsEvaluatedResult(messageId, EvaluatorResult.PASS)));

            _advisoryChangedNotifier.Handle(existingState, CreateTlsResultsEvaluatedWithResults(newTlsRecords), new List<string> { "test.gov.uk" });

            A.CallTo(() => _messageDispatcher.Dispatch(A<TlsAdvisoryAdded>._, A<string>._))
                .MustNotHaveHappened();

            A.CallTo(() => _messageDispatcher.Dispatch(A<TlsAdvisoryRemoved>._, A<string>._))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => _messageDispatcher.Dispatch(
                    A<TlsAdvisoryRemoved>.That.Matches(x => x.Messages.First().Text == errorMessage), A<string>._))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public void NotifiesAdvisoryRemovedWhenPreviouslyWarningButNowAllPass()
        {
            Guid messageId = Guid.Parse("9f200bc1-bf50-4df6-a34d-5278a82e2245");

            string errorMessage = "When testing TLS 1.2 with a range of cipher suites in reverse order the " +
                "server selected a different cipher suite (TLS_RSA_WITH_3DES_EDE_CBC_SHA) which has no " +
                "Perfect Forward Secrecy (PFS) and uses 3DES and SHA-1. The server should choose the same " +
                "cipher suite regardless of the order that they are presented by the client.";

            TlsEntityState existingState = CreateOneAdvisoryEntityStateFromExample();

            TlsRecords newTlsRecords =
                CreateTlsRecords(
                    new TlsRecord(new TlsEvaluatedResult(messageId, EvaluatorResult.PASS)));

            _advisoryChangedNotifier.Handle(existingState, CreateTlsResultsEvaluatedWithResults(newTlsRecords), new List<string> { "test.gov.uk" });

            A.CallTo(() => _messageDispatcher.Dispatch(A<TlsAdvisoryAdded>._, A<string>._))
                .MustNotHaveHappened();

            A.CallTo(() => _messageDispatcher.Dispatch(A<TlsAdvisoryRemoved>._, A<string>._))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => _messageDispatcher.Dispatch(
                    A<TlsAdvisoryRemoved>.That.Matches(x => x.Messages.First().Text == errorMessage && x.Id == "test.gov.uk" && x.Host == "mailchecktest.host.gov.uk."), A<string>._))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public void NotifiesAdvisoryAddedWhenPreviouslyPassButNowWarning()
        {
            Guid messageId = Guid.Parse("9f200bc1-bf50-4df6-a34d-5278a82e2245");

            string errorMessage = "When testing TLS 1.2 with a range of cipher suites in reverse order the " +
                "server selected a different cipher suite (TLS_RSA_WITH_3DES_EDE_CBC_SHA) which has no " +
                "Perfect Forward Secrecy (PFS) and uses 3DES and SHA-1. The server should choose the same " +
                "cipher suite regardless of the order that they are presented by the client.";

            TlsEntityState existingState = CreatePassingEntityStateFromExample();

            TlsRecords newTlsRecords =
                CreateTlsRecords(
                    new TlsRecord(new TlsEvaluatedResult(messageId, EvaluatorResult.WARNING, errorMessage)));

            _advisoryChangedNotifier.Handle(existingState, CreateTlsResultsEvaluatedWithResults(newTlsRecords), new List<string> { "test.gov.uk" });

            A.CallTo(() => _messageDispatcher.Dispatch(A<TlsAdvisoryAdded>._, A<string>._))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => _messageDispatcher.Dispatch(A<TlsAdvisoryRemoved>._, A<string>._))
                .MustNotHaveHappened();

            A.CallTo(() => _messageDispatcher.Dispatch(
                    A<TlsAdvisoryAdded>.That.Matches(x => x.Messages.First().Text == errorMessage && x.Id == "test.gov.uk" && x.Host == "mailchecktest.host.gov.uk."), A<string>._))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public void NoNotifiersWhenSameAdvisories()
        {
            Guid messageId1 = Guid.Parse("9f200bc1-bf50-4df6-a34d-5278a82e2245");

            string errorMessage = "When testing TLS 1.2 with a range of cipher suites in reverse order the " +
                "server selected a different cipher suite (TLS_RSA_WITH_3DES_EDE_CBC_SHA) which has no " +
                "Perfect Forward Secrecy (PFS) and uses 3DES and SHA-1. The server should choose the same " +
                "cipher suite regardless of the order that they are presented by the client.";

            TlsEntityState existingState = CreateOneAdvisoryEntityStateFromExample();

            TlsRecords newTlsRecords =
                CreateTlsRecords(
                    new TlsRecord(new TlsEvaluatedResult(messageId1, EvaluatorResult.WARNING, errorMessage)));

            _advisoryChangedNotifier.Handle(existingState, CreateTlsResultsEvaluatedWithResults(newTlsRecords), new List<string> { "test.gov.uk" });

            A.CallTo(() => _messageDispatcher.Dispatch(A<TlsAdvisoryAdded>._, A<string>._))
                .MustNotHaveHappened();

            A.CallTo(() => _messageDispatcher.Dispatch(A<TlsAdvisoryRemoved>._, A<string>._))
                .MustNotHaveHappened();
        }

        [Test]
        public void NotifiesAdvisoryAddedWhenPreviouslyOneAdvisoryButNowTwo()
        {
            Guid messageId1 = Guid.Parse("9f200bc1-bf50-4df6-a34d-5278a82e2245");
            Guid messageId2 = Guid.Parse("2065e53f-e44f-487d-85be-dce4e43c0758");

            string errorMessage1 = "When testing TLS 1.2 with a range of cipher suites in reverse order the " +
                "server selected a different cipher suite (TLS_RSA_WITH_3DES_EDE_CBC_SHA) which has no " +
                "Perfect Forward Secrecy (PFS) and uses 3DES and SHA-1. The server should choose the same " +
                "cipher suite regardless of the order that they are presented by the client.";

            string errorMessage2 = "When testing TLS 1.0 we were unable to create a connection";
            TlsEntityState existingState = CreateOneAdvisoryEntityStateFromExample();
            TlsRecords newTlsRecords =
                CreateTlsRecords(
                    new TlsRecord(new TlsEvaluatedResult(messageId1, EvaluatorResult.WARNING, errorMessage1)),
                    new TlsRecord(new TlsEvaluatedResult(messageId2, EvaluatorResult.FAIL, errorMessage2)));

            _advisoryChangedNotifier.Handle(existingState, CreateTlsResultsEvaluatedWithResults(newTlsRecords), new List<string> { "test.gov.uk" });

            A.CallTo(() => _messageDispatcher.Dispatch(A<TlsAdvisoryAdded>._, A<string>._))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => _messageDispatcher.Dispatch(A<TlsAdvisoryRemoved>._, A<string>._))
                .MustNotHaveHappened();

            A.CallTo(() => _messageDispatcher.Dispatch(
                    A<TlsAdvisoryAdded>.That.Matches(x => x.Messages.First().Text == errorMessage2 && x.Id == "test.gov.uk" && x.Host == "mailchecktest.host.gov.uk."), A<string>._))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public void NotifiesAdvisoryRemovedWhenPreviouslyTwoAdvisoryButNowOne()
        {
            Guid messageId1 = Guid.Parse("9f200bc1-bf50-4df6-a34d-5278a82e2245");

            string errorMessage1 = "When testing TLS 1.2 with a range of cipher suites in reverse order the " +
                "server selected a different cipher suite (TLS_RSA_WITH_3DES_EDE_CBC_SHA) which has no " +
                "Perfect Forward Secrecy (PFS) and uses 3DES and SHA-1. The server should choose the same " +
                "cipher suite regardless of the order that they are presented by the client.";

            string errorMessage2 = "When testing TLS 1.0 we were unable to create a connection";
            TlsEntityState existingState = CreateTwoAdvisoryEntityStateFromExample();
            TlsRecords newTlsRecords =
                CreateTlsRecords(
                    new TlsRecord(new TlsEvaluatedResult(messageId1, EvaluatorResult.WARNING, errorMessage1)));

            _advisoryChangedNotifier.Handle(existingState, CreateTlsResultsEvaluatedWithResults(newTlsRecords), new List<string> { "test.gov.uk" });

            A.CallTo(() => _messageDispatcher.Dispatch(A<TlsAdvisoryAdded>._, A<string>._))
                .MustNotHaveHappened();

            A.CallTo(() => _messageDispatcher.Dispatch(A<TlsAdvisoryRemoved>._, A<string>._))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => _messageDispatcher.Dispatch(
                    A<TlsAdvisoryRemoved>.That.Matches(x => x.Messages.First().Text == errorMessage2 && x.Id == "test.gov.uk" && x.Host == "mailchecktest.host.gov.uk."), A<string>._))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public void NotifiesAdvisoryRemovedAndAdvisoryAddedWhenSameAmountButDifferentAdvisories()
        {
            Guid messageId = Guid.NewGuid();

            string errorMessage1 = "When testing TLS 1.2 with a range of cipher suites in reverse order " +
                "we were unable to create a connection to the mail server. We will keep trying, so please check back later.";

            string errorMessage2 = "When testing TLS 1.1 we were unable to create a connection";

            string errorMessage3 = "When testing TLS 1.2 with a range of cipher suites in reverse order the " +
                "server selected a different cipher suite (TLS_RSA_WITH_3DES_EDE_CBC_SHA) which has no " +
                "Perfect Forward Secrecy (PFS) and uses 3DES and SHA-1. The server should choose the same " +
                "cipher suite regardless of the order that they are presented by the client.";

            string errorMessage4 = "When testing TLS 1.0 we were unable to create a connection";

            TlsEntityState existingState = CreateTwoAdvisoryEntityStateFromExample();
            TlsRecords newTlsRecords =
                CreateTlsRecords(
                    new TlsRecord(new TlsEvaluatedResult(messageId, EvaluatorResult.INCONCLUSIVE, errorMessage1)),
                    new TlsRecord(new TlsEvaluatedResult(messageId, EvaluatorResult.FAIL, errorMessage2)));

            _advisoryChangedNotifier.Handle(existingState, CreateTlsResultsEvaluatedWithResults(newTlsRecords), new List<string> { "test.gov.uk" });

            A.CallTo(() => _messageDispatcher.Dispatch(A<TlsAdvisoryAdded>._, A<string>._))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => _messageDispatcher.Dispatch(A<TlsAdvisoryRemoved>._, A<string>._))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => _messageDispatcher.Dispatch(
                    A<TlsAdvisoryAdded>.That.Matches(x => x.Messages[0].Text == errorMessage1 && x.Id == "test.gov.uk" && x.Host == "mailchecktest.host.gov.uk."), A<string>._))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => _messageDispatcher.Dispatch(
                    A<TlsAdvisoryAdded>.That.Matches(x => x.Messages[1].Text == errorMessage2 && x.Id == "test.gov.uk" && x.Host == "mailchecktest.host.gov.uk."), A<string>._))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => _messageDispatcher.Dispatch(
                    A<TlsAdvisoryRemoved>.That.Matches(x => x.Messages[0].Text == errorMessage3 && x.Id == "test.gov.uk" && x.Host == "mailchecktest.host.gov.uk."), A<string>._))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => _messageDispatcher.Dispatch(
                    A<TlsAdvisoryRemoved>.That.Matches(x => x.Messages[1].Text == errorMessage4 && x.Id == "test.gov.uk" && x.Host == "mailchecktest.host.gov.uk."), A<string>._))
                .MustHaveHappenedOnceExactly();
        }

        private TlsEntityState CreatePassingEntityStateFromExample()
        {
            string text = ExampleStateResources.NoAdvisoryExampleState;
            TlsEntityState result = JsonConvert.DeserializeObject<TlsEntityState>(text);

            return result;
        }

        private TlsEntityState CreateOneAdvisoryEntityStateFromExample()
        {
            string text = ExampleStateResources.OneAdvisoryExampleState;
            TlsEntityState result = JsonConvert.DeserializeObject<TlsEntityState>(text);

            return result;
        }

        private TlsEntityState CreateTwoAdvisoryEntityStateFromExample()
        {
            string text = ExampleStateResources.TwoAdvisoryExampleState;
            TlsEntityState result = JsonConvert.DeserializeObject<TlsEntityState>(text);

            return result;
        }

        private TlsEntityState CreateEntityStateWithMessages(TlsRecords records = null)
        {
            TlsEntityState entityState = new TlsEntityState("hostName")
            {
                TlsRecords = records ?? CreateTlsRecords()
            };

            return entityState;
        }

        private TlsRecords CreateTlsRecords(TlsRecord tls12AvailableWithBestCipherSuiteSelected = null,
            TlsRecord tls12AvailableWithBestCipherSuiteSelectedFromReverseList = null,
            TlsRecord tls12AvailableWithSha2HashFunctionSelected = null,
            TlsRecord tls12AvailableWithWeakCipherSuiteNotSelected = null,
            TlsRecord tls11AvailableWithBestCipherSuiteSelected = null,
            TlsRecord tls11AvailableWithWeakCipherSuiteNotSelected = null,
            TlsRecord tls10AvailableWithBestCipherSuiteSelected = null,
            TlsRecord tls10AvailableWithWeakCipherSuiteNotSelected = null,
            TlsRecord ssl3FailsWithBadCipherSuite = null,
            TlsRecord tlsSecureEllipticCurveSelected = null,
            TlsRecord tlsSecureDiffieHellmanGroupSelected = null,
            TlsRecord tlsWeakCipherSuitesRejected = null,
            TlsRecord tls12Available = null,
            TlsRecord tls11Available = null,
            TlsRecord tls10Available = null)
        {
            return new TlsRecords(
                tls12AvailableWithBestCipherSuiteSelected ??
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                tls12AvailableWithBestCipherSuiteSelectedFromReverseList ??
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                tls12AvailableWithSha2HashFunctionSelected ??
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                tls12AvailableWithWeakCipherSuiteNotSelected ??
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                tls11AvailableWithBestCipherSuiteSelected ??
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                tls11AvailableWithWeakCipherSuiteNotSelected ??
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                tls10AvailableWithBestCipherSuiteSelected ??
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                tls10AvailableWithWeakCipherSuiteNotSelected ??
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                ssl3FailsWithBadCipherSuite ??
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                tlsSecureEllipticCurveSelected ??
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                tlsSecureDiffieHellmanGroupSelected ??
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                tlsWeakCipherSuitesRejected ??
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                tls12Available ?? new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                tls11Available ?? new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                tls10Available ?? new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)));

        }

        private TlsResultsEvaluated CreateTlsResultsEvaluatedWithResults(TlsRecords records = null)
        {
            TlsResultsEvaluated recordsEvaluated = new TlsResultsEvaluated("mailchecktest.host.gov.uk", false, records ?? CreateTlsRecords());
            return recordsEvaluated;
        }
    }
}
