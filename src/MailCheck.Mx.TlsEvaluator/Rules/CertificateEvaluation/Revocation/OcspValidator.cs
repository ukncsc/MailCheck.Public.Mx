using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Ocsp;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Ocsp;
using Org.BouncyCastle.X509;
using X509Certificate = MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Domain.X509Certificate;
using BcX509Certificate = Org.BouncyCastle.X509.X509Certificate;

namespace MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Revocation
{
    public interface IOcspValidator
    {
        Task<RevocationResult> CheckOcspRevocation(string host, X509Certificate peerCertificate, X509Certificate issuerCertificate);
    }

    public class OcspValidator : IOcspValidator
    {
        private readonly ILogger<OcspValidator> _log;
        private readonly X509CertificateParser _certificateParser;

        public OcspValidator(ILogger<OcspValidator> log)
        {
            _log = log;
            _certificateParser = new X509CertificateParser();
        }

        public async Task<RevocationResult> CheckOcspRevocation(string host, X509Certificate peerCertificate, X509Certificate issuerCertificate)
        {
            BcX509Certificate bcPeerCertificate = _certificateParser.ReadCertificate(peerCertificate.Raw);
            BcX509Certificate bcIssuerCertificate = _certificateParser.ReadCertificate(issuerCertificate.Raw);
            
            List<string> urls = GetOcspEndPoints(bcPeerCertificate);

            if (!urls.Any())
            {
                _log.LogWarning("No urls present in Authority Info Access extension for host {Host} certificate {CommonName}", host, peerCertificate.CommonName);
                return new RevocationResult("No urls present in Authority Info Access extension");
            }

            RevocationResult result = null;

            foreach (var url in urls)
            {
                result = await GetOcspResponse(url, host, bcPeerCertificate, bcIssuerCertificate);
                if (result.Revoked.HasValue)
                {
                    return result;
                }
            }

            return result;
        }

        private async Task<RevocationResult> GetOcspResponse(string url, string host, BcX509Certificate peerCertificate, BcX509Certificate issuerCertificate)
        {
            int maxAttemptCount = 2;
            int attemptCount = 0;
            string error = null;
            while (maxAttemptCount > attemptCount)
            {
                try
                {
                    OcspReq request = GenerateOcspRequest(issuerCertificate, peerCertificate.SerialNumber);

                    _log.LogInformation("Attempt {Attempt}: Getting OCSP repsonse for host {Host} certificate {Certificate} from url {Url}",
                        attemptCount, host, peerCertificate.SubjectDN, url);

                    HttpResponseMessage httpResponseMessage = await url
                        .WithTimeout(TimeSpan.FromSeconds(20))
                        .WithHeaders(new { Content_Type = "application/ocsp-request", Accept = "application/ocsp-response" })
                        .PostAsync(new ByteArrayContent(request.GetEncoded()));

                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        OcspResp ocspResp = new OcspResp(await httpResponseMessage.Content.ReadAsStreamAsync());
                        if (ocspResp.Status == 0)
                        {
                            BasicOcspResp basicOcspResp = (BasicOcspResp)ocspResp.GetResponseObject();

                            List<RevocationInfo> revocationInfos = GetRevocationInfos(basicOcspResp);

                            bool revoked = basicOcspResp.Responses[0].GetCertStatus() != null;

                            if (revocationInfos.Any())
                            {
                                _log.LogInformation("Certificate {Certificate} for host {Host} is {RevocationStatus} with reasons {RevocationReasons}.", 
                                    peerCertificate.SubjectDN, host, revoked ? "revoked" : "not revoked", string.Join(", ", revocationInfos));
                            }
                            else
                            {
                                _log.LogInformation("Certificate {Certificate} for host {Host} is {RevocationStatus}.", 
                                    peerCertificate.SubjectDN, host, revoked ? "revoked" : "not revoked");
                            }

                            return new RevocationResult(revoked, revocationInfos);
                        }

                        error = $"OCSP response had status: {GetOcspErrorCode(ocspResp.Status)}.";

                        _log.LogWarning("Got failed OCSP revocation response for host {Host} certificate {Certificate} with ocsp error {OCSPError}",
                            host, peerCertificate.SubjectDN.ToString(), ocspResp.Status);
                    }
                    else
                    {
                        _log.LogWarning("Failed to get OCSP revocation response for host {Host} certificate {Certificate} from url {Url} with http status code {StatusCode}", 
                            host, peerCertificate.SubjectDN.ToString(), url, httpResponseMessage.StatusCode);

                        error = $"OCSP validator failed call to {url} with http status code: {httpResponseMessage.StatusCode}.";
                    }
                }
                catch (Exception e)
                {
                    _log.LogError("Failed to get OCSP revocation response for host {Host} certificate {Certificate} from url {Url} with exception {ExceptionMessage}{StackTrace}", 
                        host, peerCertificate.SubjectDN.ToString(), url, e.Message, e.StackTrace);

                    error = e.Message;
                }

                attemptCount++;
            }
            return new RevocationResult(error);
        }

        private List<RevocationInfo> GetRevocationInfos(BasicOcspResp basicOcspResp)
        {
            List<RevocationInfo> responses = new List<RevocationInfo>();

            foreach (var response in basicOcspResp.Responses)
            {
                object status = response.GetCertStatus();
                switch (status)
                {
                    case null:
                    case UnknownStatus unknownStatus:
                        responses.Add(RevocationInfo.UnknownRevocationInfo);
                        break;
                    case RevokedStatus revokedStatus:
                        var item2 = revokedStatus.HasRevocationReason ? new CrlReason(revokedStatus.RevocationReason).ToString() : "Unknown";
                        responses.Add(new RevocationInfo(revokedStatus.RevocationTime, item2));
                        break;
                    default:
                        responses.Add(RevocationInfo.UnknownRevocationInfo);
                        break;
                }
            }

            return responses;
        }

        private string GetOcspErrorCode(int errorCode)
        {
            switch (errorCode)
            {
                case 1:
                    return "Malformed request";
                case 2:
                    return "Internal error";
                case 3:
                    return "Try Later";
                case 5:
                    return "Signature Required";
                case 6:
                    return "Unauthorized";
                default:
                    return "Unknown";
            }
        }

        private List<string> GetOcspEndPoints(BcX509Certificate x509Certificate)
        {
            Asn1OctetString aiaAsn1OctetString = x509Certificate.GetExtensionValue(X509Extensions.AuthorityInfoAccess);

            if (aiaAsn1OctetString == null)
            {
                return new List<string>();
            }

            Asn1InputStream aiaAsn1InputStream = new Asn1InputStream(aiaAsn1OctetString.GetOctets());
            Asn1Object aiaAsn1Object = aiaAsn1InputStream.ReadObject();

            AuthorityInformationAccess authorityInformationAccess = AuthorityInformationAccess.GetInstance(aiaAsn1Object);
            authorityInformationAccess.GetAccessDescriptions();

            //want url not issuing cert
            return authorityInformationAccess.GetAccessDescriptions()
                .Where(_ => _.AccessMethod.Id == "1.3.6.1.5.5.7.48.1")
                .Select(_ => _.AccessLocation.Name.ToString())
                .ToList();
        }

        private OcspReq GenerateOcspRequest(BcX509Certificate issuerCertificate, BigInteger serialNumber)
        {
            CertificateID id = new CertificateID(CertificateID.HashSha1, issuerCertificate, serialNumber);

            OcspReqGenerator generator = new OcspReqGenerator();
            generator.AddRequest(id);

            Dictionary<DerObjectIdentifier, X509Extension> dictionary = new Dictionary<DerObjectIdentifier, X509Extension>
            {
                {OcspObjectIdentifiers.PkixOcspNonce,
                    new X509Extension(false, new DerOctetString(BigInteger.ValueOf(DateTime.UtcNow.Ticks).ToByteArray()))}
            };

            generator.SetRequestExtensions(new X509Extensions(dictionary));

            return generator.Generate();
        }
    }
}
