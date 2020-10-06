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


        [SetUp]
        public void SetUp()
        {
            _messageDispatcher = A.Fake<IMessageDispatcher>();
            _tlsEntityConfig = A.Fake<ITlsEntityConfig>();
            _messageEqualityComparer = new MessageEqualityComparer();

            _advisoryChangedNotifier =
                new AdvisoryChangedNotifier(_messageDispatcher, _tlsEntityConfig, _messageEqualityComparer);
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

            _advisoryChangedNotifier.Handle(existingState, CreateTlsResultsEvaluatedWithResulsts(newTlsRecords));

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

            _advisoryChangedNotifier.Handle(existingState, CreateTlsResultsEvaluatedWithResulsts(newTlsRecords));

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

            _advisoryChangedNotifier.Handle(existingState, CreateTlsResultsEvaluatedWithResulsts(newTlsRecords));

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

            _advisoryChangedNotifier.Handle(existingState, CreateTlsResultsEvaluatedWithResulsts(newTlsRecords));

            A.CallTo(() => _messageDispatcher.Dispatch(A<TlsAdvisoryAdded>._, A<string>._))
                .MustNotHaveHappened();

            A.CallTo(() => _messageDispatcher.Dispatch(A<TlsAdvisoryRemoved>._, A<string>._))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => _messageDispatcher.Dispatch(
                    A<TlsAdvisoryRemoved>.That.Matches(x => x.Messages.First().Text == errorMessage), A<string>._))
                .MustHaveHappenedOnceExactly();
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
        
        private TlsResultsEvaluated CreateTlsResultsEvaluatedWithResulsts(TlsRecords records = null)
        {
            TlsResultsEvaluated recordsEvaluated = new TlsResultsEvaluated("hostName", false, records ?? CreateTlsRecords());
            return recordsEvaluated;
        }
    }
}
