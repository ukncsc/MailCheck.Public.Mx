using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Mx.Contracts.External;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Tester;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Domain;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Util;
using Microsoft.Extensions.Logging;

namespace MailCheck.Mx.TlsEvaluator.Rules
{
    public interface ICertificateEvaluatorHandler
    {
        Task<CertificateResults> Process(TlsTestResults certificateResultMessage);
    }
    public class CertificateEvaluatorHandler  : ICertificateEvaluatorHandler
    {
        private readonly IEvaluator<HostCertificates> _evaluator;
        private readonly ILogger<CertificateEvaluatorHandler> _log;

        public CertificateEvaluatorHandler(IEvaluator<HostCertificates> evaluator,
            ILogger<CertificateEvaluatorHandler> log)

        {
            _evaluator = evaluator;
            _log = log;
        }

        public async Task<CertificateResults> Process(TlsTestResults certificateResultMessage)
        {
            string hostName = certificateResultMessage.Id.ToLower();

            _log.LogInformation("Evaluating certificates for hostName {hostName}", hostName);

            HostCertificates hostCertificates = certificateResultMessage.MapToHostCertificates();

            try
            {
                EvaluationResult<HostCertificates> results = await Evaluate(hostCertificates);

                return  results.MapToHostResults();
            
            }
            catch (Exception e)
            {
                string formatString =
                    $"Error occured evaluating certificates for hostName {{hostName}} {{ExceptionMessage}} {Environment.NewLine} {{StackTrace}}";

                _log.LogError(formatString, hostName, e.Message, e.StackTrace);

                throw;
            }
        }

       
        private async Task<EvaluationResult<HostCertificates>> Evaluate(HostCertificates hostCertificates)
        {
            if (hostCertificates != null && hostCertificates.Host == "." )
            {
               return new EvaluationResult<HostCertificates>(hostCertificates, new List<EvaluationError>());
            }

            return hostCertificates != null && hostCertificates.HostNotFound
                    ? await GetHostNotFoundResult(hostCertificates)
                    : await _evaluator.Evaluate(hostCertificates);
        }

        private static Task<EvaluationResult<HostCertificates>>
            GetHostNotFoundResult(HostCertificates hostCertificates) =>
            Task.FromResult(new EvaluationResult<HostCertificates>(hostCertificates,
                new List<EvaluationError>
                {
                    new EvaluationError(EvaluationErrorType.Error, $"The host {hostCertificates.Host} does not exist.")
                }));
    }
}