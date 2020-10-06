using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.DomainStatus.Contracts;
using MailCheck.Mx.Contracts.Entity;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.TlsEvaluator;
using MailCheck.Mx.TlsEntity.Config;
using MailCheck.Mx.TlsEntity.Dao;
using Microsoft.Extensions.Logging;

namespace MailCheck.Mx.TlsEntity.Entity.DomainStatus
{
    public interface IDomainStatusPublisher
    {
        Task Publish(TlsResultsEvaluated message);
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

        public async Task Publish(TlsResultsEvaluated message)
        {
            List<TlsEvaluatedResult> evaluatedResults =
                message.TlsRecords?.Records?.Select(x => x.TlsEvaluatedResult).ToList();
            List<Error> evaluatedCertificates = message.Certificates?.Errors;

            Dictionary<string, List<TlsEntityState>> domainsContainingHost = await _dao.GetDomains(message.Id);

            foreach (string domain in domainsContainingHost.Keys)
            {
                List<TlsEntityState> existingHostStates = domainsContainingHost[domain];
                List<TlsEvaluatedResult> existingResults = existingHostStates
                    .Where(x => x.TlsRecords?.Records != null)
                    .SelectMany(x => x.TlsRecords?.Records?.Select(y => y.TlsEvaluatedResult)).ToList();

                List<Error> existingCertificates = existingHostStates
                    .Where(x => x.CertificateResults?.Errors != null)
                    .SelectMany(x => x.CertificateResults?.Errors).ToList();

                existingResults.AddRange(evaluatedResults ?? new List<TlsEvaluatedResult>());

                existingCertificates.AddRange(evaluatedCertificates ?? new List<Error>());

                Status status = _domainStatusEvaluator.GetStatus(existingResults, existingCertificates);

                DomainStatusEvaluation domainStatusEvaluation = new DomainStatusEvaluation(domain, "TLS", status);

                _log.LogInformation(
                    $"Publishing TLS domain status for domain {domain} because it contains mx host {message.Id} which was evaluated");
                _dispatcher.Dispatch(domainStatusEvaluation, _tlsEntityConfig.SnsTopicArn);
            }
        }
    }
}