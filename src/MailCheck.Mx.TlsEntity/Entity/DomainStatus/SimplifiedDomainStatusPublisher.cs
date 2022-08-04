using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MailCheck.Common.Contracts.Advisories;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.DomainStatus.Contracts;
using MailCheck.Mx.TlsEntity.Config;
using MailCheck.Mx.TlsEntity.Dao;
using Microsoft.Extensions.Logging;

namespace MailCheck.Mx.TlsEntity.Entity.DomainStatus
{
    public interface ISimplifiedDomainStatusPublisher
    {
        Task CalculateAndPublishDomainStatuses(string ipAddress);
    }

    public class SimplifiedDomainStatusPublisher : ISimplifiedDomainStatusPublisher
    {
        private readonly IMessageDispatcher _dispatcher;
        private readonly ITlsEntityConfig _tlsEntityConfig;
        private readonly ISimplifiedTlsEntityDao _dao;
        private readonly ILogger<SimplifiedDomainStatusPublisher> _log;

        public SimplifiedDomainStatusPublisher(IMessageDispatcher dispatcher, ITlsEntityConfig tlsEntityConfig, ISimplifiedTlsEntityDao dao, ILogger<SimplifiedDomainStatusPublisher> log)
        {
            _dispatcher = dispatcher;
            _tlsEntityConfig = tlsEntityConfig;
            _dao = dao;
            _log = log;
        }

        public async Task CalculateAndPublishDomainStatuses(string ipAddress)
        {
            Dictionary<string, Status> domainsWithHostsErrors = await _dao.GetMaxAdvisoryStatusesForAffectedDomainsByMxHostIp(ipAddress);

            foreach (var (domain, status) in domainsWithHostsErrors)
            {
                DomainStatusEvaluation domainStatusEvaluation = new DomainStatusEvaluation(domain, _tlsEntityConfig.RecordType, status);
                _dispatcher.Dispatch(domainStatusEvaluation, _tlsEntityConfig.SnsTopicArn);
            }

            _log.LogInformation($"Published {domainsWithHostsErrors.Count} TLS domain status messages");
        }
    }
}
