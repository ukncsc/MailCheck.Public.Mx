using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.X509;
using X509Certificate = MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Domain.X509Certificate;
using BcX509Certificate = Org.BouncyCastle.X509.X509Certificate;

namespace MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Revocation
{
    public interface ICrlValidator
    {
        Task<RevocationResult> CheckCrlRevocation(string host, X509Certificate peerCertificate);
    }

    public class CrlValidator : ICrlValidator
    {
        private readonly ILogger<CrlValidator> _log;
        private readonly X509CrlParser _x509CrlParser;
        private readonly X509CertificateParser _certificateParser;

        public CrlValidator(ILogger<CrlValidator> log)
        {
            _log = log;
            _x509CrlParser = new X509CrlParser();
            _certificateParser = new X509CertificateParser();
        }

        public async Task<RevocationResult> CheckCrlRevocation(string host, X509Certificate peerCertificate)
        {
            BcX509Certificate bcPeerCertificate = _certificateParser.ReadCertificate(peerCertificate.Raw);
            List<string> urls = GetCrlDistPoints(bcPeerCertificate);
            if (!urls.Any())
            {
                _log.LogWarning("No urls present in crl distribution point extension for host {Host} certificate {CommonName}", host, peerCertificate.CommonName);
                return new RevocationResult("No urls present in crl distribution point extension");
            }

            RevocationResult result = null;

            foreach (var url in urls)
            {
                result = await GetCrlResponse(url, host, bcPeerCertificate);
                if (result.Revoked.HasValue)
                {
                    return result;
                }
            }

            return result;
        }

        private async Task<RevocationResult> GetCrlResponse(string url, string host, BcX509Certificate peerCertificate)
        {
            int maxAttemptCount = 2;
            int attemptCount = 0;
            string error = null;
            while (maxAttemptCount > attemptCount)
            {
                try
                {
                    _log.LogInformation("Attempt {Attempt}: Getting crl info for host {Host} certificate {Certificate} from url {Url}",
                        attemptCount, host, peerCertificate.SubjectDN, url);

                    HttpResponseMessage httpResponseMessage = await url
                        .WithTimeout(TimeSpan.FromSeconds(20))
                        .GetAsync();

                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        X509Crl x509Crl = _x509CrlParser.ReadCrl(await httpResponseMessage.Content.ReadAsStreamAsync());

                        bool revoked = x509Crl.IsRevoked(peerCertificate);

                        List<RevocationInfo> revocationInfos = new List<RevocationInfo>();
                        if (revoked)
                        {
                            X509CrlEntry crlEntry = x509Crl.GetRevokedCertificate(peerCertificate.SerialNumber);
                            string revocationReason = "unknown";
                            if (crlEntry.HasExtensions)
                            {
                                revocationReason = crlEntry.GetExtensionValue(X509Extensions.ReasonCode)?.ToString() ?? "unknown";
                            }
                            revocationInfos.Add(new RevocationInfo(crlEntry.RevocationDate, revocationReason));

                            _log.LogInformation("Certificate {Certificate} for host {Host} is was revoked on {RevocationDate} with reason {RevocationReason}.",
                                peerCertificate.SubjectDN, host,  crlEntry.RevocationDate, revocationReason);
                        }
                        else
                        {
                            _log.LogInformation("Certificate {Certificate} for host {Host} is not revoked.",
                                peerCertificate.SubjectDN, host);
                        }

                        return new RevocationResult(revoked, revocationInfos);
                    }

                    _log.LogWarning("Failed to get crl info for host {Host} certificate {Certificate} from url {Url} with http status code {StatusCode}",
                        host, peerCertificate.SubjectDN.ToString(), url, httpResponseMessage.StatusCode);

                    error = $"Failed call to {url} with http status code {httpResponseMessage.StatusCode}.";
                }
                catch (Exception e)
                {
                    _log.LogError("Failed to get crl revocation info for host {Host} certificate {Certificate} from url {Url} with exception {ExceptionMessage}{StackTrace}",
                        host, peerCertificate.SubjectDN.ToString(), url, e.Message, e.StackTrace);

                    error = e.Message;
                }

                attemptCount++;
            }
            return new RevocationResult(error);
        }


        private List<string> GetCrlDistPoints(BcX509Certificate x509Certificate)
        {
            Asn1OctetString crldpAsn1OctetString = x509Certificate.GetExtensionValue(X509Extensions.CrlDistributionPoints);

            if (crldpAsn1OctetString == null)
            {
                return new List<string>();
            }

            Asn1InputStream crldpAsn1InputStream = new Asn1InputStream(crldpAsn1OctetString.GetOctets());
            Asn1Object crldpAsn1Object = crldpAsn1InputStream.ReadObject();

            return CrlDistPoint.GetInstance(crldpAsn1Object).GetDistributionPoints()
                .Select(_ => _.DistributionPointName)
                .Where(_ => _.PointType == DistributionPointName.FullName)
                .SelectMany(_ => GeneralNames.GetInstance(_.Name).GetNames())
                .Where(_ => _.TagNo == GeneralName.UniformResourceIdentifier)
                .Select(_ => _.Name.ToString())
                .ToList();
        }
    }
}