using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        private readonly TimeSpan _certificateDictionaryTtl = TimeSpan.FromDays(7);
        private Dictionary<string, X509Certificate> _certificateDictionary;
        private DateTime _certificateDictionaryCacheExpiryTime = DateTime.MinValue;

        public RootCertificateLookUp(IRootCertificateProvider rootCertificateProvider, IClock clock)
        {
            _rootCertificateProvider = rootCertificateProvider;
            _clock = clock;
        }

        public async Task<X509Certificate> GetCertificate(string issuer)
        {
            DateTime now = _clock.GetDateTimeUtc();
            if (_certificateDictionary == null || _certificateDictionaryCacheExpiryTime <= now)
            {
                List<X509Certificate> x509Certificates = await _rootCertificateProvider.GetRootCaCertificates();
                _certificateDictionary = x509Certificates.Where(_ => _.Issuer.Trim() == _.Subject.Trim()).ToDictionary(_ => _.Issuer.Trim().ToLower());
                _certificateDictionaryCacheExpiryTime = now.Add(_certificateDictionaryTtl);
            }

            _certificateDictionary.TryGetValue(issuer.Trim().ToLower(), out var cert);
            return cert;
        }
    }
}