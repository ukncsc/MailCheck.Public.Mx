using System;
using System.Collections.Generic;
using System.Linq;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Tester;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Domain;
using Error = MailCheck.Mx.Contracts.SharedDomain.Error;

namespace MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Util
{
    public static class MappingExtensions
    {
      
        public static HostCertificates MapToHostCertificates(this TlsTestResults tlsTestResults)
        {
            return new HostCertificates(tlsTestResults.Id, 
                    tlsTestResults.HostNotFound,
                    tlsTestResults.Certificates.MapToX509Certificates(),
                    tlsTestResults.SelectedCipherSuites
                    );
        }
        
        private static List<X509Certificate> MapToX509Certificates(this List<string> certificates)
        {
            return certificates
                .Select(_ => new X509Certificate(Convert.FromBase64String(_)))
                .ToList();
        }

        public static CertificateResults MapToHostResults(this EvaluationResult<HostCertificates> results)
        {
            List<Certificate> certificates = results.Item.Certificates.MapToCertificate();

            List<Error> errors = results.Errors.MapToErrors();

            return new CertificateResults(certificates, errors);
        }

        public static List<Error> MapToErrors(this IEnumerable<EvaluationError> evaluationErrors)
        {
            return evaluationErrors
                .Select(_ => new Error((ErrorType)_.ErrorType, _.Message, _.Markdown))
                .ToList();
        }

        public static List<Certificate> MapToCertificate(this IEnumerable<X509Certificate> certificates)
        {
            return certificates
                .Select(_ => new Certificate(_.ThumbPrint,
                    _.Issuer,
                    _.Subject,
                    _.ValidFrom,
                    _.ValidTo,
                    _.KeyAlgoritm,
                    _.KeyLength,
                    _.SerialNumber,
                    _.Version,
                    _.SubjectAlternativeName,
                    _.CommonName))
                .ToList();
        }
    }
}
