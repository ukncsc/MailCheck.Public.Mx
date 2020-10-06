using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Domain;
using Microsoft.Extensions.Logging;

namespace MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Rules
{
    public class CertificateShouldMatchHostName : IRule<HostCertificates>
    {
        private readonly ILogger<CertificateShouldMatchHostName> _log;
        private readonly Regex _dnsName = new Regex("(dns name=|dns:)(?<dnsname>[^\\s,]+)", RegexOptions.IgnoreCase | RegexOptions.Multiline);

        public CertificateShouldMatchHostName(ILogger<CertificateShouldMatchHostName> log)
        {
            _log = log;
        }

        public Task<List<EvaluationError>> Evaluate(HostCertificates hostCertificates)
        {
            _log.LogInformation("Running rule {RuleNumber}:{Rule} for host {Host}", SequenceNo, nameof(CertificateShouldMatchHostName), hostCertificates.Host);
            X509Certificate certificate = hostCertificates.Certificates.First();

            string host = hostCertificates.Host.Trim().TrimEnd('.').ToLower();

            bool certificateValidForHost = false;

            if (!string.IsNullOrWhiteSpace(certificate.CommonName) && Regex.IsMatch(host, CreateWildCardRegex(certificate.CommonName)))
            {
                certificateValidForHost = true;
            }

            if (!string.IsNullOrWhiteSpace(certificate.SubjectAlternativeName))
            {
                MatchCollection matches = _dnsName.Matches(certificate.SubjectAlternativeName);
                List<string> dnsNameMatches =
                    matches.Select(_ => _.Groups["dnsname"].Value.Trim().TrimEnd('.').ToLower()).ToList();

                if (dnsNameMatches.Any(_ => Regex.IsMatch(host, CreateWildCardRegex(_))))
                {
                    certificateValidForHost = true;
                }
            }

            List<EvaluationError> list = certificateValidForHost
                ? new List<EvaluationError>()
                : new List<EvaluationError>
                {
                    new EvaluationError(EvaluationErrorType.Error,
                        string.Format(CertificateEvaluatorErrors.CertificateShouldMatchHostName, certificate.CommonName,
                            certificate.SubjectAlternativeName ?? "<null>", host))
                };


            return Task.FromResult(list);
        }

        private string CreateWildCardRegex(string wildCard) => $"^{Regex.Escape(wildCard).Replace("\\*", "[a-zA-Z0-9](\\-?[a-zA-Z0-9]){0,64}")}$";

        public int SequenceNo => 2;
        public bool IsStopRule => false;
    }
}
