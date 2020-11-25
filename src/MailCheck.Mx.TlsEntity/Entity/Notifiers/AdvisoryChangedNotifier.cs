using System;
using System.Collections.Generic;
using System.Linq;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Mx.Contracts.Entity;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.TlsEvaluator;
using MailCheck.Mx.TlsEntity.Config;
using MailCheck.Mx.TlsEntity.Entity.Notifications;
using Microsoft.Extensions.Logging;
using Message = MailCheck.Common.Messaging.Abstractions.Message;

namespace MailCheck.Mx.TlsEntity.Entity.Notifiers
{
    public class AdvisoryChangedNotifier : IChangeNotifier
    {
        private readonly IMessageDispatcher _dispatcher;
        private readonly ITlsEntityConfig _tlsEntityConfig;
        private readonly IEqualityComparer<TlsEvaluatedResult> _messageEqualityComparer;
        private readonly ILogger<AdvisoryChangedNotifier> _logger;

        public AdvisoryChangedNotifier(IMessageDispatcher dispatcher, ITlsEntityConfig tlsEntityConfig,
            IEqualityComparer<TlsEvaluatedResult> messageEqualityComparer, ILogger<AdvisoryChangedNotifier> logger)
        {
            _dispatcher = dispatcher;
            _tlsEntityConfig = tlsEntityConfig;
            _messageEqualityComparer = messageEqualityComparer;
            _logger = logger;
        }

        public void Handle(TlsEntityState state, Message message, List<string> domains)
        {
            if (message is TlsResultsEvaluated evaluationResult)
            {
                List<TlsEvaluatedResult> currentMessages = GetAdvisoryMessages(state.TlsRecords);

                List<TlsEvaluatedResult> newMessages = GetAdvisoryMessages(evaluationResult.TlsRecords);

                _logger.LogInformation(
                    $"Evaluation Result messages count: {newMessages.Count}");

                List<TlsEvaluatedResult> addedMessages =
                    newMessages.Except(currentMessages).ToList();

                if (addedMessages.Any())
                {
                    foreach (string domain in domains)
                    {
                        TlsAdvisoryAdded advisoryAdded = new TlsAdvisoryAdded(domain, state.Id,
                        addedMessages.Select(x => new AdvisoryMessage(GetMessageType(x.Result.Value), x.Description))
                            .ToList());
                        _dispatcher.Dispatch(advisoryAdded, _tlsEntityConfig.SnsTopicArn);
                        _logger.LogInformation(
                            $"TlsAdvisoryAdded message dispatched to {_tlsEntityConfig.SnsTopicArn} for domain: {domain} and host: {message.Id}");
                    }
                }
                else
                {
                    _logger.LogInformation($"No new TlsAdvisoryAdded found for host: {message.Id}");
                }

                List<TlsEvaluatedResult> removedMessages =
                    currentMessages.Except(newMessages).ToList();
                if (removedMessages.Any())
                {
                    foreach (string domain in domains)
                    {
                        TlsAdvisoryRemoved advisoryRemoved = new TlsAdvisoryRemoved(domain, state.Id,
                        removedMessages.Select(x => new AdvisoryMessage(GetMessageType(x.Result.Value), x.Description))
                            .ToList());
                        _dispatcher.Dispatch(advisoryRemoved, _tlsEntityConfig.SnsTopicArn);
                        _logger.LogInformation(
                            $"TlsAdvisoryRemoved message dispatched to {_tlsEntityConfig.SnsTopicArn} for domain: {domain} and host: {message.Id}");
                    }
                }
                else
                {
                    _logger.LogInformation($"No new TlsAdvisoryRemoved found for host: {message.Id}");
                }
            }
        }
    
        private List<TlsEvaluatedResult> GetAdvisoryMessages(TlsRecords tlsRecords)
        {
            List<TlsEvaluatedResult> messages = new List<TlsEvaluatedResult>();

            if (tlsRecords != null)
            {
                AddAdvisoryMessage(messages,
                    tlsRecords.Tls12AvailableWithBestCipherSuiteSelected.TlsEvaluatedResult);
                AddAdvisoryMessage(messages,
                    tlsRecords.Tls12AvailableWithBestCipherSuiteSelectedFromReverseList.TlsEvaluatedResult);
                AddAdvisoryMessage(messages, tlsRecords.Tls12AvailableWithSha2HashFunctionSelected.TlsEvaluatedResult);
                AddAdvisoryMessage(messages,
                    tlsRecords.Tls12AvailableWithWeakCipherSuiteNotSelected.TlsEvaluatedResult);
                AddAdvisoryMessage(messages, tlsRecords.Tls11AvailableWithBestCipherSuiteSelected.TlsEvaluatedResult);
                AddAdvisoryMessage(messages,
                    tlsRecords.Tls11AvailableWithWeakCipherSuiteNotSelected.TlsEvaluatedResult);
                AddAdvisoryMessage(messages, tlsRecords.Tls10AvailableWithBestCipherSuiteSelected.TlsEvaluatedResult);
                AddAdvisoryMessage(messages,
                    tlsRecords.Tls10AvailableWithWeakCipherSuiteNotSelected.TlsEvaluatedResult);
                AddAdvisoryMessage(messages, tlsRecords.Ssl3FailsWithBadCipherSuite.TlsEvaluatedResult);
                AddAdvisoryMessage(messages, tlsRecords.TlsSecureEllipticCurveSelected.TlsEvaluatedResult);
                AddAdvisoryMessage(messages, tlsRecords.TlsSecureDiffieHellmanGroupSelected.TlsEvaluatedResult);
                AddAdvisoryMessage(messages, tlsRecords.TlsWeakCipherSuitesRejected.TlsEvaluatedResult);
                AddAdvisoryMessage(messages, tlsRecords.Tls12Available.TlsEvaluatedResult);
                AddAdvisoryMessage(messages, tlsRecords.Tls11Available.TlsEvaluatedResult);
                AddAdvisoryMessage(messages, tlsRecords.Tls10Available.TlsEvaluatedResult);
            }

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
