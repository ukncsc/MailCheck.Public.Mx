using System;
using System.Collections.Generic;
using System.Linq;
using MailCheck.Common.Contracts.Advisories;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Mx.Contracts.Entity;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.TlsEvaluator;
using MailCheck.Mx.TlsEntity.Config;
using MailCheck.Mx.TlsEntity.Entity.Notifications;
using Microsoft.Extensions.Logging;
using Message = MailCheck.Common.Messaging.Abstractions.Message;
using MessageType = MailCheck.Common.Contracts.Advisories.MessageType;

namespace MailCheck.Mx.TlsEntity.Entity.Notifiers
{
    public class AdvisoryChangedNotifier : IChangeNotifier
    {
        private readonly IMessageDispatcher _messageDispatcher;
        private readonly ITlsEntityConfig _tlsEntityConfig;
        private readonly ILogger<AdvisoryChangedNotifier> _log;

        public AdvisoryChangedNotifier(
            IMessageDispatcher messageDispatcher,
            ITlsEntityConfig tlsEntityConfig,
            ILogger<AdvisoryChangedNotifier> log)
        {
            _messageDispatcher = messageDispatcher;
            _tlsEntityConfig = tlsEntityConfig;
            _log = log;
        }

        public void Handle(TlsEntityState state, Message message, List<string> domains)
        {
            if (message is TlsResultsEvaluated evaluationResult)
            {
                string host = state.Id;

                List<AdvisoryMessage> addedConfigAdvisories = new List<AdvisoryMessage>();

                List<AdvisoryMessage> sustainedConfigAdvisories = new List<AdvisoryMessage>();

                List<AdvisoryMessage> removedConfigAdvisories = new List<AdvisoryMessage>();

                List<AdvisoryMessage> addedCertAdvisories = new List<AdvisoryMessage>();

                List<AdvisoryMessage> sustainedCertAdvisories = new List<AdvisoryMessage>();

                List<AdvisoryMessage> removedCertAdvisories = new List<AdvisoryMessage>();

                _log.LogInformation("Getting TLS config advisories.");

                Advisories<TlsEvaluatedResult> configAdvisories = new Advisories<TlsEvaluatedResult>(ExtractMessages(state?.TlsRecords), ExtractMessages(evaluationResult?.TlsRecords));

                addedConfigAdvisories.AddRange(configAdvisories.Added.Select(x => AdvisoryFactory.Create(GetMessageTypeFromConfigMessage(x), x.Description)).ToList());

                sustainedConfigAdvisories.AddRange(configAdvisories.Sustained.Select(x => AdvisoryFactory.Create(GetMessageTypeFromConfigMessage(x), x.Description)).ToList());

                removedConfigAdvisories.AddRange(configAdvisories.Removed.Select(x => AdvisoryFactory.Create(GetMessageTypeFromConfigMessage(x), x.Description)).ToList());


                _log.LogInformation("Getting TLS certificate advisories.");

                Advisories<Error> certAdvisories = new Advisories<Error>(
                    state?.CertificateResults?.Errors,
                    evaluationResult?.Certificates?.Errors
                );

                addedCertAdvisories.AddRange(certAdvisories.Added.Select(x => AdvisoryFactory.Create(GetMessageTypeFromCertError(x), x.Message)).ToList());

                sustainedCertAdvisories.AddRange(certAdvisories.Sustained.Select(x => AdvisoryFactory.Create(GetMessageTypeFromCertError(x), x.Message)).ToList());

                removedCertAdvisories.AddRange(certAdvisories.Removed.Select(x => AdvisoryFactory.Create(GetMessageTypeFromCertError(x), x.Message)).ToList());


                if (addedConfigAdvisories.Any())
                {
                    domains.ForEach(x => _messageDispatcher.Dispatch(new TlsAdvisoryAdded(x, host, addedConfigAdvisories), _tlsEntityConfig.SnsTopicArn));
                    _log.LogInformation($"Dispatched {domains.Count} TlsAdvisoryAdded messages which contain {addedConfigAdvisories.Count} advisories");
                }

                if (sustainedConfigAdvisories.Any())
                {
                    domains.ForEach(x => _messageDispatcher.Dispatch(new TlsAdvisorySustained(x, host, sustainedConfigAdvisories), _tlsEntityConfig.SnsTopicArn));
                    _log.LogInformation($"Dispatched {domains.Count} TlsAdvisorySustained messages which contain {sustainedConfigAdvisories.Count} advisories");
                }

                if (removedConfigAdvisories.Any())
                {
                    domains.ForEach(x => _messageDispatcher.Dispatch(new TlsAdvisoryRemoved(x, host, removedConfigAdvisories), _tlsEntityConfig.SnsTopicArn));
                    _log.LogInformation($"Dispatched {domains.Count} TlsAdvisoryRemoved messages which contain {removedConfigAdvisories.Count} advisories");
                }

                if (addedCertAdvisories.Any())
                {
                    domains.ForEach(x => _messageDispatcher.Dispatch(new TlsCertAdvisoryAdded(x, host, addedCertAdvisories), _tlsEntityConfig.SnsTopicArn));
                    _log.LogInformation($"Dispatched {domains.Count} TlsAdvisoryAdded messages which contain {addedCertAdvisories.Count} advisories");
                }

                if (sustainedCertAdvisories.Any())
                {
                    domains.ForEach(x => _messageDispatcher.Dispatch(new TlsCertAdvisorySustained(x, host, sustainedCertAdvisories), _tlsEntityConfig.SnsTopicArn));
                    _log.LogInformation($"Dispatched {domains.Count} TlsAdvisorySustained messages which contain {sustainedCertAdvisories.Count} advisories");
                }

                if (removedCertAdvisories.Any())
                {
                    domains.ForEach(x => _messageDispatcher.Dispatch(new TlsCertAdvisoryRemoved(x, host, removedCertAdvisories), _tlsEntityConfig.SnsTopicArn));
                    _log.LogInformation($"Dispatched {domains.Count} TlsAdvisoryRemoved messages which contain {removedCertAdvisories.Count} advisories");
                }
            }
        }

        private MessageType GetMessageTypeFromCertError(Error x)
        {
            switch (x.ErrorType)
            {
                case ErrorType.Error:
                    return MessageType.error;
                case ErrorType.Warning:
                    return MessageType.warning;
                case ErrorType.Inconclusive:
                    return MessageType.info;
            }

            throw new InvalidOperationException($"unsupported error type: {x.ErrorType}");
        }

        private MessageType GetMessageTypeFromConfigMessage(TlsEvaluatedResult tlsEvaluatedResult)
        {
            if (tlsEvaluatedResult != null)
            {
                switch (tlsEvaluatedResult.Result)
                {
                    case EvaluatorResult.PASS:
                        return MessageType.success;
                    case EvaluatorResult.FAIL:
                        return MessageType.error;
                    case EvaluatorResult.WARNING:
                        return MessageType.warning;
                    case EvaluatorResult.INCONCLUSIVE:
                    case EvaluatorResult.INFORMATIONAL:
                        return MessageType.info;
                    default:
                        _log.LogError($"Invalid tlsEvaluatedResult: {tlsEvaluatedResult.Result}");
                        throw new InvalidOperationException($"unsupported result type: {tlsEvaluatedResult.Result}");
                }
            }
            else
            {
                _log.LogError("tlsEvaluatedResult is null");
                throw new InvalidOperationException($"tlsEvaluatedResult is null");
            }
        }

        private IEnumerable<TlsEvaluatedResult> ExtractMessages(TlsRecords tlsRecords)
        {
            if ((tlsRecords?.Records?.Count ?? 0) == 0)
            {
                return Enumerable.Empty<TlsEvaluatedResult>();
            }

            return tlsRecords.Records
                .Select(record => record.TlsEvaluatedResult)
                .Where(tlsEvalResult => !string.IsNullOrWhiteSpace(tlsEvalResult.Description))
                .Where(tlsEvalResult =>
                {
                    var result = tlsEvalResult.Result;
                    return result == EvaluatorResult.FAIL ||
                           result == EvaluatorResult.INCONCLUSIVE ||
                           result == EvaluatorResult.INFORMATIONAL ||
                           result == EvaluatorResult.WARNING;
                })
                .ToList();
        }
    }
}