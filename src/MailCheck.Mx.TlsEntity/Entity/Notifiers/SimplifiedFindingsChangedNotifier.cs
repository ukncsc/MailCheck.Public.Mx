using System.Collections.Generic;
using System.Linq;
using MailCheck.Common.Contracts.Findings;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Common.Processors.Notifiers;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.TlsEntity.Config;
using Microsoft.Extensions.Logging;
using MessageType = MailCheck.Common.Contracts.Advisories.MessageType;

namespace MailCheck.Mx.TlsEntity.Entity.Notifiers
{
    public interface ISimplifiedFindingsChangedNotifier
    {
        void Handle(string host, List<string> domains, string path, IEnumerable<NamedAdvisory> currentAdvisories, IEnumerable<NamedAdvisory> newAdvisories);
    }

    public class SimplifiedFindingsChangedNotifier : ISimplifiedFindingsChangedNotifier
    {
        private readonly IMessageDispatcher _dispatcher;
        private readonly ITlsEntityConfig _tlsEntityConfig;
        private readonly IFindingsChangedNotifier _findingsChangedCalculator;
        private readonly ILogger<SimplifiedFindingsChangedNotifier> _log;

        public SimplifiedFindingsChangedNotifier(IMessageDispatcher dispatcher, ITlsEntityConfig tlsEntityConfig,
            IFindingsChangedNotifier findingsChangedCalculator, ILogger<SimplifiedFindingsChangedNotifier> log)
        {
            _dispatcher = dispatcher;
            _tlsEntityConfig = tlsEntityConfig;
            _findingsChangedCalculator = findingsChangedCalculator;
            _log = log;
        }

        public void Handle(string host, List<string> domains, string path, IEnumerable<NamedAdvisory> currentAdvisories, IEnumerable<NamedAdvisory> newAdvisories)
        {

            List<Finding> tlsCurrentFindings = ExtractFindingsFromMessages(currentAdvisories ?? new List<NamedAdvisory>());
            List<Finding> tlsNewFindings = ExtractFindingsFromMessages(newAdvisories ?? new List<NamedAdvisory>());

            foreach (Finding finding in tlsCurrentFindings.Concat(tlsNewFindings))
            {
                finding.Title = finding.Title += $" (Host: {host}).";
            }

            foreach (string domain in domains)
            {
                FindingsChanged findingsChanged = _findingsChangedCalculator.Process(
                    domain,
                    "TLS",
                    EnrichFindings(domain, host, path, tlsCurrentFindings),
                    EnrichFindings(domain, host, path, tlsNewFindings));

                if (findingsChanged.Added?.Count > 0 || findingsChanged.Sustained?.Count > 0 || findingsChanged.Removed?.Count > 0)
                {
                    _log.LogInformation($"Dispatching {path} FindingsChanged for domain: {domain} and host: {host}: {findingsChanged.Added?.Count} findings added, {findingsChanged.Sustained?.Count} findings sustained, {findingsChanged.Removed?.Count} findings removed");
                    _dispatcher.Dispatch(findingsChanged, _tlsEntityConfig.SnsTopicArn);
                }
                else
                {
                    _log.LogInformation($"No {path} Findings to dispatch for domain: {domain} and host: {host}");
                }
            }
        }

        private List<Finding> ExtractFindingsFromMessages(IEnumerable<NamedAdvisory> advisories)
        {
            List<Finding> findings = advisories.Select(advisory => new Finding
            {
                Name = advisory.Name,
                Title = advisory.Text,
                Severity = AdvisoryMessageTypeToFindingSeverityMapping[advisory.MessageType]
            }).ToList();

            return findings;
        }

        private IList<Finding> EnrichFindings(string domain, string host, string path, List<Finding> findings)
        {
            foreach (Finding finding in findings)
            {
                finding.EntityUri = $"domain:{domain}|host:{host}";
                finding.SourceUrl = $"https://{_tlsEntityConfig.WebUrl}/app/domain-security/{domain}/{path}/{host}";
            }

            return findings;
        }

        internal static readonly Dictionary<MessageType, string> AdvisoryMessageTypeToFindingSeverityMapping = new Dictionary<MessageType, string>
        {
            [MessageType.info] = "Informational",
            [MessageType.warning] = "Advisory",
            [MessageType.error] = "Urgent",
            [MessageType.success] = "Positive",
        };

    }
}