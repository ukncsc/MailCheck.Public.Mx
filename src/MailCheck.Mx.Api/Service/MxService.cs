using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MailCheck.Common.Contracts.Messaging;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Mx.Api.Config;
using MailCheck.Mx.Api.Dao;
using MailCheck.Mx.Api.Domain;
using MailCheck.Mx.Contracts.Entity;
using Microsoft.Extensions.Logging;

namespace MailCheck.Mx.Api.Service
{
    public interface IMxService
    {
        Task<DomainTlsEvaluatorResults> GetDomainTlsEvaluatorResults(string domain);
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
            MxEntityState mxState = await _mxApiDao.GetMxEntityState(domain);

            if (mxState == null)
            {
                _log.LogInformation($"Domain {domain} not found - publishing DomainMissing");
                await _messagePublisher.Publish(new DomainMissing(domain), _config.MicroserviceOutputSnsTopicArn);
                return null;
            }

            if (mxState.HostMxRecords == null)
            {
                return _domainTlsEvaluatorResultsFactory.CreatePending(domain);
            }

            List<string> hostNames = mxState.HostMxRecords.Select(x => x.Id).ToList();

            Dictionary<string, TlsEntityState> tlsEntityStates = await _mxApiDao.GetTlsEntityStates(hostNames);

            DomainTlsEvaluatorResults result = _domainTlsEvaluatorResultsFactory.Create(mxState, tlsEntityStates);
            return result;
        }
    }
}