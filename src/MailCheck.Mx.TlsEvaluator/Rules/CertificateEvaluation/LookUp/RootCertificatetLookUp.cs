using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using X509Certificate = MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Domain.X509Certificate;

namespace MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.LookUp
{
    public interface IRootCertificateLookUp
    {
        Task<X509Certificate> GetCertificate(string issuer);
    }

    public class RootCertificateLookUp : IRootCertificateLookUp
    {
        private readonly IRootCertificateProvider _rootCertificateProvider;
        private readonly IClock _clock;
        private readonly ILogger<RootCertificateLookUp> _logger;
        private readonly TimeSpan _certificateDictionaryTtl = TimeSpan.FromDays(7);
        private Dictionary<string, X509Certificate> _certificateDictionary;
        private DateTime _certificateDictionaryCacheExpiryTime = DateTime.MinValue;

        public RootCertificateLookUp(IRootCertificateProvider rootCertificateProvider, IClock clock, ILogger<RootCertificateLookUp> logger)
        {
            _rootCertificateProvider = rootCertificateProvider;
            _clock = clock;
            _logger = logger;
        }

        public async Task<X509Certificate> GetCertificate(string issuer)
        {
            DateTime now = _clock.GetDateTimeUtc();
            if (_certificateDictionary == null || _certificateDictionaryCacheExpiryTime <= now)
            {
                await RefreshCache(now);
            }

            if (!_certificateDictionary.TryGetValue(issuer.Trim().ToLower(), out X509Certificate cert))
            {
                _logger.LogWarning($"Issuer not found: {issuer}");
            }

            return cert;
        }

        private async Task RefreshCache(DateTime now)
        {
            List<X509Certificate> x509Certificates = await _rootCertificateProvider.GetRootCaCertificates();

            var issuerGroups = x509Certificates
                .Select(cert => new { IssuerName = cert.Issuer.Trim().ToLower(), SubjectName = cert.Subject.Trim().ToLower(), Cert = cert })
                .Where(certWithNames => certWithNames.IssuerName == certWithNames.SubjectName)
                .GroupBy(certWithNames => certWithNames.IssuerName)
                .ToDictionary(grouping => grouping.Key, grouping => grouping.ToList());

            var badIssuers = issuerGroups
                .Where(issuerGroup => issuerGroup.Value.Count > 1)
                .Select(issuerGroup => issuerGroup.Key)
                .ToArray();

            _logger.LogWarning($"Duplicate issuers detected in root CA certs - things may not be working quite right.{Environment.NewLine}{string.Join(Environment.NewLine, badIssuers)}");

            _certificateDictionary = issuerGroups
                .ToDictionary(issuerGroup => issuerGroup.Key, issuerGroup => issuerGroup.Value[0].Cert);

            _certificateDictionaryCacheExpiryTime = now.Add(_certificateDictionaryTtl);
        }
    }
}