using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.DomainStatus.Contracts;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.TlsEntity.Config;
using MailCheck.Mx.TlsEntity.Dao;
using Microsoft.Extensions.Logging;

namespace MailCheck.Mx.TlsEntity.Entity.DomainStatus
{
    public interface IDomainStatusPublisher
    {
        Task Publish(string host);
    }

    public class NullDomainStatusPublisher : IDomainStatusPublisher
    {
        public Task Publish(string host)
        {
            return Task.CompletedTask;
        }
    }

    public class DomainStatusPublisher : IDomainStatusPublisher
    {
        private readonly IMessageDispatcher _dispatcher;
        private readonly ITlsEntityConfig _tlsEntityConfig;
        private readonly ITlsEntityDao _dao;
        private readonly IDomainStatusEvaluator _domainStatusEvaluator;
        private readonly ILogger<DomainStatusPublisher> _log;

        public DomainStatusPublisher(IMessageDispatcher dispatcher, ITlsEntityConfig tlsEntityConfig, ITlsEntityDao dao, IDomainStatusEvaluator domainStatusEvaluator, ILogger<DomainStatusPublisher> log)
        {
            _dispatcher = dispatcher;
            _tlsEntityConfig = tlsEntityConfig;
            _dao = dao;
            _domainStatusEvaluator = domainStatusEvaluator;
            _log = log;
        }

        public async Task Publish(string host)
        {
            Dictionary<string, List<HostErrors>> domainsWithHostsErrors = await _dao.GetRelatedDomainsWithErrors(host);

            foreach (KeyValuePair<string, List<HostErrors>> keyValuePair in domainsWithHostsErrors)
            {
                string domain = keyValuePair.Key;
                List<HostErrors> associatedHostsErrors = keyValuePair.Value;

                List<EvaluatorResult?> existingResults = associatedHostsErrors
                    .SelectMany(x => x.ConfigErrors).ToList();

                List<Error> existingCertificates = associatedHostsErrors
                    .SelectMany(x => x.CertErrors).ToList();

                Status status = _domainStatusEvaluator.GetStatus(existingResults, existingCertificates);

                DomainStatusEvaluation domainStatusEvaluation = new DomainStatusEvaluation(domain, _tlsEntityConfig.RecordType, status);

                _log.LogInformation(
                    $"Publishing TLS domain status for domain {domain} because it contains mx host {host} which was evaluated");
                _dispatcher.Dispatch(domainStatusEvaluation, _tlsEntityConfig.SnsTopicArn);
            }
        }
    }
}