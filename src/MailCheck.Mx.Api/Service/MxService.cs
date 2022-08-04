using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MailCheck.Common.Contracts.Messaging;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Mx.Api.Config;
using MailCheck.Mx.Api.Dao;
using MailCheck.Mx.Api.Domain;
using MailCheck.Mx.Contracts.Entity;
using MailCheck.Mx.Contracts.External;
using MailCheck.Mx.Contracts.Simplified;
using Microsoft.Extensions.Logging;

namespace MailCheck.Mx.Api.Service
{
    public interface IMxService
    {
        Task<DomainTlsEvaluatorResults> GetDomainTlsEvaluatorResults(string domain);

        public Task<bool> RecheckTls(string domain);
    }

    public class MxService : IMxService
    {
        private readonly IMxApiDao _mxApiDao;
        private readonly IDomainTlsEvaluatorResultsFactory _domainTlsEvaluatorResultsFactory;
        private readonly IMessagePublisher _messagePublisher;
        private readonly IMxApiConfig _config;
        private readonly ILogger<MxService> _log;

        public MxService(IMxApiDao mxApiDao, IDomainTlsEvaluatorResultsFactory domainTlsEvaluatorResultsFactory, IMessagePublisher messagePublisher, IMxApiConfig config, ILogger<MxService> log)
        {
            _mxApiDao = mxApiDao;
            _domainTlsEvaluatorResultsFactory = domainTlsEvaluatorResultsFactory;
            _messagePublisher = messagePublisher;
            _config = config;
            _log = log;
        }

        public async Task<DomainTlsEvaluatorResults> GetDomainTlsEvaluatorResults(string domain)
        {
            List<SimplifiedTlsEntityState> simplifiedStates = (await _mxApiDao.GetSimplifiedStates(domain)) ?? new List<SimplifiedTlsEntityState>();

            if (simplifiedStates.Count() == 0)
            {
                MxEntityState mxState = await _mxApiDao.GetMxEntityState(domain);

                if (mxState == null)
                {
                    _log.LogInformation($"Mx entity state does not exist for domain {domain} - publishing DomainMissing");
                    await _messagePublisher.Publish(new DomainMissing(domain), _config.MicroserviceOutputSnsTopicArn);
                    return null;
                }

                if (mxState.HostMxRecords == null)
                {
                    return _domainTlsEvaluatorResultsFactory.CreatePending(domain);
                }

                if (mxState.HostMxRecords.Count == 0)
                {
                    return _domainTlsEvaluatorResultsFactory.CreateNoMx(domain);
                }

                return _domainTlsEvaluatorResultsFactory.CreateNoTls(domain);
            }

            Dictionary<string, int> preferences = await _mxApiDao.GetPreferences(domain);
            DomainTlsEvaluatorResults results =
                _domainTlsEvaluatorResultsFactory.Create(domain, preferences, simplifiedStates);

            return results;
        }

        public async Task<bool> RecheckTls(string domain)
        {
            DomainTlsEvaluatorResults result = await GetDomainTlsEvaluatorResults(domain);
            DateTime recentCutoffDate = DateTime.Now.AddSeconds(_config.RecheckMinPeriodInSeconds);

            bool recentlyChecked = result.AssociatedIps.Any(x => x.TlsLastUpdated > recentCutoffDate);
            if (!recentlyChecked)
            {
                _log.LogInformation($"Check TLS requested for {domain}");

                var distinctIps = result.AssociatedIps.Select(x => x.IpAddress).Distinct();
                foreach (var ipAddress in distinctIps)
                {
                    _log.LogInformation($"Publishing SimplifiedTlsExpired for ip: {ipAddress}");
                    SimplifiedTlsExpired recheck = new SimplifiedTlsExpired(Guid.NewGuid().ToString(), ipAddress);
                    await _messagePublisher.Publish(recheck, _config.SnsTopicArn);
                }

                return true;
            }

            return false;
        }
    }
}