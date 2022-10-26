using System.Collections.Generic;
using MailCheck.Common.Contracts.Findings;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Entity.Config;
using MessageType = MailCheck.Common.Contracts.Advisories.MessageType;

namespace MailCheck.Mx.Entity.Entity
{
    public interface IFindingFactory
    {
        Finding Create(NamedAdvisory advisory, string domain, string host);
    }

    public class FindingFactory : IFindingFactory
    {
        private readonly IMxEntityConfig _config;

        public FindingFactory(IMxEntityConfig config)
        {
            _config = config;
        }

        public Finding Create(NamedAdvisory advisory, string domain, string host)
        {
            Finding finding = new Finding
            {
                EntityUri = $"domain:{domain}|host:{host}",
                Name = advisory.Name,
                Severity = AdvisoryMessageTypeToFindingSeverityMapping[advisory.MessageType],
                SourceUrl = $"https://{_config.WebUrl}/app/domain-security/{domain}/TLS/{host}",
                Title = $"{advisory.Text} (Host: {host})."
            };
            return finding;
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