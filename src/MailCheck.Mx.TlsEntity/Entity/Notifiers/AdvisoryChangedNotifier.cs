using System;
using System.Collections.Generic;
using System.Linq;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Mx.Contracts.Entity;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.TlsEvaluator;
using MailCheck.Mx.TlsEntity.Config;
using MailCheck.Mx.TlsEntity.Entity.Notifications;
using Message = MailCheck.Common.Messaging.Abstractions.Message;

namespace MailCheck.Mx.TlsEntity.Entity.Notifiers
{
    public class AdvisoryChangedNotifier : IChangeNotifier
    {
        private readonly IMessageDispatcher _dispatcher;
        private readonly ITlsEntityConfig _tlsEntityConfig;
        private readonly IEqualityComparer<TlsEvaluatedResult> _messageEqualityComparer;

        public AdvisoryChangedNotifier(IMessageDispatcher dispatcher, ITlsEntityConfig tlsEntityConfig,
            IEqualityComparer<TlsEvaluatedResult> messageEqualityComparer)
        {
            _dispatcher = dispatcher;
            _tlsEntityConfig = tlsEntityConfig;
            _messageEqualityComparer = messageEqualityComparer;
        }

        public void Handle(TlsEntityState state, Message message)
        {
            if (message is TlsResultsEvaluated evaluationResult)
            {
                List<TlsEvaluatedResult> currentMessages = GetAdvisoryMessages(state.TlsRecords);

                List<TlsEvaluatedResult> newMessages = GetAdvisoryMessages(evaluationResult.TlsRecords);

                List<TlsEvaluatedResult> addedMessages = newMessages.Except(currentMessages, _messageEqualityComparer).ToList();

                if (addedMessages.Any())
                {
                    TlsAdvisoryAdded advisoryAdded = new TlsAdvisoryAdded(state.Id, addedMessages.Select(x => new AdvisoryMessage(GetMessageType(x.Result.Value), x.Description)).ToList());
                    _dispatcher.Dispatch(advisoryAdded, _tlsEntityConfig.SnsTopicArn);
                }

                List<TlsEvaluatedResult> removedMessages = currentMessages.Except(newMessages, _messageEqualityComparer).ToList();
                if (removedMessages.Any())
                {
                    TlsAdvisoryRemoved advisoryRemoved = new TlsAdvisoryRemoved(state.Id, removedMessages.Select(x => new AdvisoryMessage(GetMessageType(x.Result.Value), x.Description)).ToList());
                    _dispatcher.Dispatch(advisoryRemoved, _tlsEntityConfig.SnsTopicArn);
                }
            }
        }
    
        private List<TlsEvaluatedResult> GetAdvisoryMessages(TlsRecords tlsRecords)
        {
            List<TlsEvaluatedResult> messages = new List<TlsEvaluatedResult>();

            AddAdvisoryMessage(messages,
                tlsRecords.Tls12AvailableWithBestCipherSuiteSelected.TlsEvaluatedResult);
            AddAdvisoryMessage(messages,
                tlsRecords.Tls12AvailableWithBestCipherSuiteSelectedFromReverseList.TlsEvaluatedResult);
            AddAdvisoryMessage(messages, tlsRecords.Tls12AvailableWithSha2HashFunctionSelected.TlsEvaluatedResult);
            AddAdvisoryMessage(messages, tlsRecords.Tls12AvailableWithWeakCipherSuiteNotSelected.TlsEvaluatedResult);
            AddAdvisoryMessage(messages, tlsRecords.Tls11AvailableWithBestCipherSuiteSelected.TlsEvaluatedResult);
            AddAdvisoryMessage(messages, tlsRecords.Tls11AvailableWithWeakCipherSuiteNotSelected.TlsEvaluatedResult);
            AddAdvisoryMessage(messages, tlsRecords.Tls10AvailableWithBestCipherSuiteSelected.TlsEvaluatedResult);
            AddAdvisoryMessage(messages, tlsRecords.Tls10AvailableWithWeakCipherSuiteNotSelected.TlsEvaluatedResult);
            AddAdvisoryMessage(messages, tlsRecords.Ssl3FailsWithBadCipherSuite.TlsEvaluatedResult);
            AddAdvisoryMessage(messages, tlsRecords.TlsSecureEllipticCurveSelected.TlsEvaluatedResult);
            AddAdvisoryMessage(messages, tlsRecords.TlsSecureDiffieHellmanGroupSelected.TlsEvaluatedResult);
            AddAdvisoryMessage(messages, tlsRecords.TlsWeakCipherSuitesRejected.TlsEvaluatedResult);
            AddAdvisoryMessage(messages, tlsRecords.Tls12Available.TlsEvaluatedResult);
            AddAdvisoryMessage(messages, tlsRecords.Tls11Available.TlsEvaluatedResult);
            AddAdvisoryMessage(messages, tlsRecords.Tls10Available.TlsEvaluatedResult);

            return messages;
        }

        private void AddAdvisoryMessage(List<TlsEvaluatedResult> messages, TlsEvaluatedResult tlsEvaluatedResult)
        {
            if (tlsEvaluatedResult != null && (tlsEvaluatedResult.Result == EvaluatorResult.FAIL
                                               || tlsEvaluatedResult.Result == EvaluatorResult.INCONCLUSIVE
                                               || tlsEvaluatedResult.Result == EvaluatorResult.INFORMATIONAL
                                               || tlsEvaluatedResult.Result == EvaluatorResult.WARNING))
            {
                messages.Add(tlsEvaluatedResult);
            }
        }

        private MessageType GetMessageType(EvaluatorResult tlsEvaluatedResult)
        {
            switch (tlsEvaluatedResult)
            {
                case EvaluatorResult.FAIL:
                    return MessageType.error;
                case EvaluatorResult.WARNING:
                    return MessageType.warning;
                case EvaluatorResult.INCONCLUSIVE:
                case EvaluatorResult.INFORMATIONAL:
                    return MessageType.info;
            }

            throw new InvalidOperationException("unsupported result type");
        }
    }
}
