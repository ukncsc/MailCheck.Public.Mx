using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using MailCheck.Common.Contracts.Advisories;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Common.Util;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Simplified;
using MailCheck.Mx.TlsEvaluator.Config;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Domain;
using Microsoft.Extensions.Logging;

namespace MailCheck.Mx.TlsEvaluator
{
    public class SimplifiedEvaluationHandler : IHandle<SimplifiedHostCertificateResult>
    {
        private readonly IEvaluator<HostCertificates> _evaluator;
        private readonly IEvaluator<HostCertificatesWithName> _namedEvaluator;
        private readonly IMessageDispatcher _dispatcher;
        private readonly ITlsRptEvaluatorConfig _config;
        private readonly ILogger<SimplifiedEvaluationHandler> _logger;
        private readonly Func<SimplifiedHostCertificateResult, List<HostCertificates>> _extractor;

        public SimplifiedEvaluationHandler(
            IEvaluator<HostCertificates> evaluator,
            IEvaluator<HostCertificatesWithName> namedEvaluator,
            IMessageDispatcher dispatcher,
            ITlsRptEvaluatorConfig config,
            ILogger<SimplifiedEvaluationHandler> logger) : this(evaluator, namedEvaluator, dispatcher, config, logger, null)
        {
        }

        internal SimplifiedEvaluationHandler(
            IEvaluator<HostCertificates> evaluator,
            IEvaluator<HostCertificatesWithName> namedEvaluator,
            IMessageDispatcher dispatcher,
            ITlsRptEvaluatorConfig config,
            ILogger<SimplifiedEvaluationHandler> logger,
            Func<SimplifiedHostCertificateResult, List<HostCertificates>> extractor)
        {
            _evaluator = evaluator;
            _namedEvaluator = namedEvaluator;
            _dispatcher = dispatcher;
            _config = config;
            _logger = logger;
            _extractor = extractor ?? ExtractCertificateEvaluationParams;
        }

        public async Task Handle(SimplifiedHostCertificateResult message)
        {
            using (_logger.BeginScope(new Dictionary<string, string>
            {
                ["IpAddress"] = message.Id,
            }))
            {
                string ipAddress = message.Id;

                var evalResult = new SimplifiedHostCertificateEvaluated(ipAddress)
                {
                    Hostnames = message.Hostnames,
                    CertificateAdvisoryMessages = new List<NamedAdvisory>(),
                    Certificates = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
                };

                var hostSpecificAdvisories = new ConcurrentDictionary<string, List<NamedAdvisory>>(StringComparer.InvariantCultureIgnoreCase);

                if (message.Certificates.Count > 0)
                {
                    List<HostCertificates> chainsWithTests = _extractor(message);

                    foreach (var chain in chainsWithTests)
                    {
                        _logger.LogInformation($"Performing evaluation for chain {string.Join(",", chain.Certificates?.Select(cert => cert.ThumbPrint) ?? Enumerable.Empty<string>())}");
                        var certResult = await _evaluator.Evaluate(chain);
                        var advisories = certResult.Errors.SelectMany(MapToAdvisory);
                        evalResult.CertificateAdvisoryMessages.AddRange(advisories);

                        AddEvaluatedCertificatesToResult(evalResult, certResult.Item.Certificates);

                        foreach (var hostname in message.Hostnames)
                        {
                            var chainWithHostname = new HostCertificatesWithName(hostname, chain);

                            var hostResult = await _namedEvaluator.Evaluate(chainWithHostname);
                            var hostAdvisories = hostResult.Errors.SelectMany(MapToAdvisory).ToList();

                            if (hostAdvisories.Count > 0)
                            {
                                var allHostAdvisories = hostSpecificAdvisories.GetOrAdd(hostname, key => new List<NamedAdvisory>());
                                allHostAdvisories.AddRange(hostAdvisories);
                            }
                        }
                    }
                }

                evalResult.HostSpecificCertificateAdvisoryMessages = new Dictionary<string, List<NamedAdvisory>>(hostSpecificAdvisories);

                foreach(var batch in Batch(evalResult))
                {
                    _logger.LogInformation($"Dispatching SimplifiedHostCertificateEvaluated message for {batch.Hostnames.Count} hosts with {batch.CertificateAdvisoryMessages.Count} global advisories.");
                    _dispatcher.Dispatch(batch, _config.SnsTopicArn);
                }
            }
        }

        internal static List<HostCertificates> ExtractCertificateEvaluationParams(SimplifiedHostCertificateResult message)
        {
            string hostname = message.Id;

            var certLookup = message.Certificates.ToDictionary(kvp => kvp.Key, kvp => new X509Certificate(Convert.FromBase64String(kvp.Value)), StringComparer.InvariantCultureIgnoreCase);

            var chainsWithTests = message
                .SimplifiedTlsConnectionResults
                .Where(conn => conn.CertificateThumbprints != null && conn.CertificateThumbprints.Length != 0)
                .GroupBy(conn => conn.CertificateThumbprints, CertsComparer.Default)
                .Select(chainKeyValuePair =>
                {
                    var certChain = chainKeyValuePair.Key.Select(chainItem => certLookup[chainItem]).ToList();
                    var selectedCiphers = chainKeyValuePair.Select(item => new SelectedCipherSuite(item.TestName, item.CipherSuite)).ToList();
                    return new HostCertificates(hostname, false, certChain, selectedCiphers);
                })
                .ToList();

            return chainsWithTests;
        }

        internal static IEnumerable<SimplifiedHostCertificateEvaluated> Batch(SimplifiedHostCertificateEvaluated message, int batchSize = 100)
        {
            if (message.HostSpecificCertificateAdvisoryMessages.Count == 0 || message.Hostnames.Count <= batchSize)
            {
                return Enumerable.Repeat(message, 1);
            }

            return message.Hostnames
                .Batch(batchSize)
                .Select(hostsBatch => {
                    var hostnames = hostsBatch.ToList();
                    return new SimplifiedHostCertificateEvaluated(message.Id)
                    {
                        Hostnames = hostnames,
                        Certificates = message.Certificates,
                        CertificateAdvisoryMessages = message.CertificateAdvisoryMessages,
                        HostSpecificCertificateAdvisoryMessages = message.HostSpecificCertificateAdvisoryMessages.Where(kvp => hostnames.Contains(kvp.Key)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                        RootCertificateThumbprint = message.RootCertificateThumbprint
                    };
                });
        }

        private IEnumerable<NamedAdvisory> MapToAdvisory(EvaluationError error)
        {
            if (error.ErrorType == EvaluationErrorType.Error || error.ErrorType == EvaluationErrorType.Warning)
            {
                yield return new NamedAdvisory(error.Id, error.Name, MapMessageType(error.ErrorType), error.Message, error.Markdown);
            }
        }

        private Common.Contracts.Advisories.MessageType MapMessageType(EvaluationErrorType errorType)
        {
            return errorType switch
            {
                EvaluationErrorType.Warning => Common.Contracts.Advisories.MessageType.warning,
                EvaluationErrorType.Error => Common.Contracts.Advisories.MessageType.error,
                _ => Common.Contracts.Advisories.MessageType.info,
            };
        }

        private class CertsComparer : EqualityComparer<string[]>
        {
            public static new readonly IEqualityComparer<string[]> Default = new CertsComparer();

            public override bool Equals([AllowNull] string[] x, [AllowNull] string[] y)
            {
                if (x.Length != y.Length) return false;

                return x.SequenceEqual(y);
            }

            public override int GetHashCode([DisallowNull] string[] obj)
            {
                return obj.FirstOrDefault()?.GetHashCode() ?? 0;
            }
        }

        private void AddEvaluatedCertificatesToResult(SimplifiedHostCertificateEvaluated evalResult, List<X509Certificate> certificates)
        {
            foreach (X509Certificate certificate in certificates ?? new List<X509Certificate>())
            {
                // if root certificate has not be poplulated then check to see if this cert is root and populate if so.
                if (evalResult.RootCertificateThumbprint == null)
                {
                    bool isRootCert = certificate.Issuer.ToLower().Trim() == certificate.Subject.ToLower().Trim();
                    if (isRootCert)
                    {
                        evalResult.RootCertificateThumbprint = certificate.ThumbPrint;
                    }
                }
                evalResult.Certificates.TryAdd(certificate.ThumbPrint, Convert.ToBase64String(certificate.Raw));
            }
        }
    }
}
