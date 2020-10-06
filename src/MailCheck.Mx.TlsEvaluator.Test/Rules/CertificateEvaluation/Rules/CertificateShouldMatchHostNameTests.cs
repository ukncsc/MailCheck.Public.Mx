using System.Collections.Generic;
using System.Threading.Tasks;
using FakeItEasy;
using MailCheck.Mx.Contracts.SharedDomain;
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
        public Task<List<EvaluationError>> Test(HostCertificates hostCertificates)
        {
            CertificateShouldMatchHostName certificateShouldMatchHostName = new CertificateShouldMatchHostName(A.Fake<ILogger<CertificateShouldMatchHostName>>());

            return certificateShouldMatchHostName.Evaluate(hostCertificates);
        }

        public static IEnumerable<TestCaseData> TestData()
        {
            yield return new TestCaseData(Create("mail.abc.xyz.com", "mail.abc.xyz.com", null)).Returns(new List<EvaluationError>()).SetName("Common name matches host name - succeeds.");
            yield return new TestCaseData(Create("mail.abc.xyz.com", "*.abc.xyz.com", null)).Returns(new List<EvaluationError>()).SetName("Common name wild card matching host - succeeds.");
            yield return new TestCaseData(Create("mail.abc.xyz.com", "*.xyz.com", "DNS Name=def.hij.com\r\nDNS Name=*.xyz.com")).Returns(new List<EvaluationError> { new EvaluationError(EvaluationErrorType.Error, "The certificate *.xyz.com with subject alternative name: DNS Name=def.hij.com\r\nDNS Name=*.xyz.com does not match host mail.abc.xyz.com.") });
            yield return new TestCaseData(Create("mail.abc.xyz.com", "def.hij.com", null)).Returns(new List<EvaluationError> { new EvaluationError(EvaluationErrorType.Error, "The certificate def.hij.com with subject alternative name: <null> does not match host mail.abc.xyz.com.") }).SetName("Common name mismatch with no SAN - fails.");
            yield return new TestCaseData(Create("mail.abc.xyz.com", "def.hij.com", "DNS Name=def.hij.com\r\nDNS Name=mail.abc.xyz.com")).Returns(new List<EvaluationError>()).SetName("Common name mismatch with matching DNS Name SAN - succeeds.");
            yield return new TestCaseData(Create("mail.abc.xyz.com", "def.hij.com", "DNS Name=def.hij.com\r\nDNS Name=*.abc.xyz.com")).Returns(new List<EvaluationError>()).SetName("Common name mismatch with wildcard matching DNS Name SAN - succeeds.");
            yield return new TestCaseData(Create("mail.abc.xyz.com", "def.hij.com", "DNS Name=def.hij.com\r\nDNS Name=hij.abc.xyz.com")).Returns(new List<EvaluationError> { new EvaluationError(EvaluationErrorType.Error, "The certificate def.hij.com with subject alternative name: DNS Name=def.hij.com\r\nDNS Name=hij.abc.xyz.com does not match host mail.abc.xyz.com.") }).SetName("Common name mismatch and SAN mismatch - fails.");
        }

        private static HostCertificates Create(string hostName, string commonName, string subjectAlternativeName)
        {
            X509Certificate certificate = A.Fake<X509Certificate>();
            A.CallTo(() => certificate.CommonName).Returns(commonName);
            A.CallTo(() => certificate.SubjectAlternativeName).Returns(subjectAlternativeName);
            return new HostCertificates(hostName, false, new List<X509Certificate> { certificate }, new List<SelectedCipherSuite>());
        }
    }
}
