using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CsvHelper;
using Flurl.Http;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Domain;
using Microsoft.Extensions.Logging;

namespace MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.LookUp
{
    public interface IRootCertificateProvider
    {
        Task<List<X509Certificate>> GetRootCaCertificates();
    }

    public class MozillaRootCertificateProvider : IRootCertificateProvider
    {
        private readonly ILogger<MozillaRootCertificateProvider> _log;
        private const string Url = "https://ccadb-public.secure.force.com/mozilla/IncludedCACertificateReportPEMCSV";

        public MozillaRootCertificateProvider(ILogger<MozillaRootCertificateProvider> log)
        {
            _log = log;
        }

        public async Task<List<X509Certificate>> GetRootCaCertificates()
        {
            _log.LogInformation("Retrieving root CA certificates from {Url}", Url);

            HttpResponseMessage httpResponseMessage = await Url.WithTimeout(TimeSpan.FromSeconds(20)).GetAsync();

            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                _log.LogError("Failed to retrieve root CA certificates from {Url} with error {Http}", Url, httpResponseMessage.StatusCode);
                throw new Exception($"Failed to retrieve root CA certificates from {Url} with error {httpResponseMessage.StatusCode}");
            }

            using (Stream stream = await httpResponseMessage.Content.ReadAsStreamAsync())
            {
                using (TextReader textReader = new StreamReader(stream))
                {
                    using (CsvReader csv = new CsvReader(textReader))
                    {
                        return csv.GetRecords<dynamic>().Select(GetCertificate).ToList();
                    }
                }
            }
        }

        private static X509Certificate GetCertificate(dynamic obj)
        {
            ExpandoObject expandoObject = obj as ExpandoObject;
            string certificateString = (string)expandoObject.FirstOrDefault(v => v.Key == "PEM Info").Value;
            certificateString = certificateString.Replace("\'-----BEGIN CERTIFICATE-----", String.Empty)
                .Replace("-----END CERTIFICATE-----\'", String.Empty)
                .Trim();

            return new X509Certificate(Convert.FromBase64String(certificateString));
        }
    }
}