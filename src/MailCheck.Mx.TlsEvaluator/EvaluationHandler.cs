using System.Linq;
using System.Threading.Tasks;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Tester;
using MailCheck.Mx.Contracts.TlsEvaluator;
using MailCheck.Mx.TlsEvaluator.Config;
using MailCheck.Mx.TlsEvaluator.Rules;

namespace MailCheck.Mx.TlsEvaluator
{
    public class EvaluationHandler : IHandle<TlsTestResults>
    {
        private readonly IEvaluationProcessor _tlsRptEvaluationProcessor;
        private readonly ICertificateEvaluatorHandler _certificateProcessor;
        private readonly IMessageDispatcher _dispatcher;
        private readonly ITlsRptEvaluatorConfig _config;

        public EvaluationHandler(IEvaluationProcessor tlsRptEvaluationProcessor,
            ICertificateEvaluatorHandler certificateProcessor,
            IMessageDispatcher dispatcher,
            ITlsRptEvaluatorConfig config)
        {
            _tlsRptEvaluationProcessor = tlsRptEvaluationProcessor;
            _certificateProcessor = certificateProcessor;
            _dispatcher = dispatcher;
            _config = config;
        }

        public async Task Handle(TlsTestResults message)
        {
            TlsResultsEvaluated results = await _tlsRptEvaluationProcessor.Process(message);

            if (message.Certificates.Any())
            {
                CertificateResults certs = await _certificateProcessor.Process(message);

                results = new TlsResultsEvaluated(results.Id, results.Failed, results.TlsRecords, certs);
            }
            
            _dispatcher.Dispatch(results, _config.SnsTopicArn);
        }
    }
}
