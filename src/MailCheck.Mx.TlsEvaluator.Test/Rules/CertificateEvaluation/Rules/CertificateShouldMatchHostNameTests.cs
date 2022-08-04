using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FakeItEasy;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Domain;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Rules;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace MailCheck.Mx.TlsEvaluator.Test.Rules.CertificateEvaluation.Rules
{
    [TestFixture]
    public class CertificateShouldMatchHostNameTests
    {
        [TestCaseSource(nameof(TestData))]
        public void Test(HostCertificatesWithName hostCertificates, List<EvaluationError> expectedErrors)
        {
            CertificateShouldMatchHostName certificateShouldMatchHostName = new CertificateShouldMatchHostName(A.Fake<ILogger<CertificateShouldMatchHostName>>());
            Task<List<EvaluationError>> result = certificateShouldMatchHostName.Evaluate(hostCertificates);

            Assert.That(result.Result.Count == expectedErrors.Count);

            if (expectedErrors.Count > 0)
            {
                for (int i = 0; i < expectedErrors.Count; i++)
                {
                    Assert.That(result.Result[i].ErrorType == expectedErrors[i].ErrorType);
                    Assert.That(result.Result[i].Message == expectedErrors[i].Message);
                    Assert.That(result.Result[i].Markdown == expectedErrors[i].Markdown);
                }
            }
        }

        public static IEnumerable<TestCaseData> TestData()
        {
            Guid guid = new Guid("729e3487-82df-47f7-a7df-d40d3cf4448e");
            string name = "mailcheck.tlsCert.certificateShouldMatchHostName";

            yield return new TestCaseData(
                Create("mail.abc.xyz.com", "127.0.0.1", "mail.abc.xyz.com", null), 
                new List<EvaluationError>()
            ).SetName("Common name matches host name - succeeds.");
            yield return new TestCaseData(
                Create("mail.abc.xyz.com", "127.0.0.1", "*.abc.xyz.com", null), 
                new List<EvaluationError>()
            ).SetName("Common name wild card matching host - succeeds.");
            yield return new TestCaseData(
                Create("mail.abc.xyz.com", "127.0.0.1", "*.xyz.com", "DNS Name=def.hij.com\r\nDNS Name=*.xyz.com"), 
                new List<EvaluationError> { 
                    new EvaluationError(
                        guid,
                        name,
                        EvaluationErrorType.Error, 
                        "Certificate host mismatch. The certificate Subject Alternative Name does not match host mail.abc.xyz.com.", 
                        string.Format(CertificateEvaluatorErrorsMarkdown.CertificateShouldMatchHostName, "mail.abc.xyz.com", "DNS Name=def.hij.com\r\nDNS Name=*.xyz.com")
                    ) 
                }
            ).SetName("Common name mismatch with wildcard SAN - fails");
            yield return new TestCaseData(
                Create("mail.abc.xyz.com", "127.0.0.1", "def.hij.com", null), 
                new List<EvaluationError> { 
                    new EvaluationError(
                        guid,
                        name,
                        EvaluationErrorType.Error, 
                        "Certificate host mismatch. The certificate Subject Alternative Name does not match host mail.abc.xyz.com.",
                        string.Format(CertificateEvaluatorErrorsMarkdown.CertificateShouldMatchHostName, "mail.abc.xyz.com", "<null>")
                    ) 
                }
            ).SetName("Common name mismatch with no SAN - fails.");
            yield return new TestCaseData(
                Create("mail.abc.xyz.com", "127.0.0.1", "def.hij.com", "DNS Name=def.hij.com\r\nDNS Name=mail.abc.xyz.com"), 
                new List<EvaluationError>()
            ).SetName("Common name mismatch with matching DNS Name SAN - succeeds.");
            yield return new TestCaseData(
                Create("mail.abc.xyz.com", "127.0.0.1", "def.hij.com", "DNS Name=def.hij.com\r\nDNS Name=*.abc.xyz.com"), 
                new List<EvaluationError>()
            ).SetName("Common name mismatch with wildcard matching DNS Name SAN - succeeds.");
            yield return new TestCaseData(
                Create("mail.abc.xyz.com", "127.0.0.1", "def.hij.com", "DNS Name=def.hij.com\r\nDNS Name=hij.abc.xyz.com"), 
                new List<EvaluationError> { 
                    new EvaluationError(
                        guid,
                        name,
                        EvaluationErrorType.Error, 
                        "Certificate host mismatch. The certificate Subject Alternative Name does not match host mail.abc.xyz.com.",
                        string.Format(CertificateEvaluatorErrorsMarkdown.CertificateShouldMatchHostName, "mail.abc.xyz.com", "DNS Name=def.hij.com\r\nDNS Name=hij.abc.xyz.com")
                    ) 
                }
            ).SetName("Common name mismatch and SAN mismatch - fails.");
        }

        private static HostCertificatesWithName Create(string hostName, string ip, string commonName, string subjectAlternativeName)
        {
            X509Certificate certificate = A.Fake<X509Certificate>();
            A.CallTo(() => certificate.CommonName).Returns(commonName);
            A.CallTo(() => certificate.SubjectAlternativeName).Returns(subjectAlternativeName);
            return new HostCertificatesWithName(hostName, new HostCertificates(ip, false, new List<X509Certificate> { certificate }, new List<SelectedCipherSuite>()));
        }
    }
}
