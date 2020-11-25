using System;
using System.Collections.Generic;
using System.Linq;
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
    public class CertificateExpiryShouldBeInDateTests
    {
        [TestCaseSource(nameof(TestData))]
        public Task<List<EvaluationError>> Test(HostCertificates hostCertificates)
        {
            CertificateExpiryShouldBeInDate rule = new CertificateExpiryShouldBeInDate(A.Fake<ILogger<CertificateExpiryShouldBeInDate>>());

            return rule.Evaluate(hostCertificates);
        }

        public static IEnumerable<TestCaseData> TestData()
        {
            HostCertificates expiredDate = Create(Create("Certificate1", DateTime.UtcNow.AddDays(-1)));
            HostCertificates sevenDaysFromToday = Create(Create("Certificate2", DateTime.UtcNow.AddDays(7)));
            HostCertificates fourteenDaysFromToday = Create(Create("Certificate3", DateTime.UtcNow.AddDays(14)));
            HostCertificates fifteenDaysFromToday = Create(Create("Certificate3", DateTime.UtcNow.AddDays(15)));
            HostCertificates thirtyDaysFromToday = Create(Create("Certificate4", DateTime.UtcNow.AddDays(30)));
            HostCertificates multipleExpiredDate = Create(Create("Certificate1", DateTime.UtcNow.AddDays(-1)), Create("Certificate2", DateTime.UtcNow.AddDays(-1)));
            HostCertificates multipleSevenDaysFromToday = Create(Create("Certificate1", DateTime.UtcNow.AddDays(7)), Create("Certificate2", DateTime.UtcNow.AddDays(7)));
            HostCertificates mixOfExpiredAndExpiring = Create(Create("Certificate1", DateTime.UtcNow.AddDays(-1)), Create("Certificate2", DateTime.UtcNow.AddDays(7)));

            yield return new TestCaseData(expiredDate).Returns(new List<EvaluationError> { new EvaluationError(EvaluationErrorType.Error, $"The certificate Certificate1 expired on {expiredDate.Certificates.First().ValidTo:dd/MM/yyyy HH:mm} and should be replaced.") })
                .SetName("Certificate expired 1 day ago - fails.");

            yield return new TestCaseData(sevenDaysFromToday).Returns(new List<EvaluationError> { new EvaluationError(EvaluationErrorType.Warning, $"The certificate Certificate2 will expire on {sevenDaysFromToday.Certificates.First().ValidTo:dd/MM/yyyy HH:mm} and should be replaced.") })
                .SetName("Certificate expires in 7 days - fails.");

            yield return new TestCaseData(fourteenDaysFromToday).Returns(new List<EvaluationError> { new EvaluationError(EvaluationErrorType.Warning, $"The certificate Certificate3 will expire on {fourteenDaysFromToday.Certificates.First().ValidTo:dd/MM/yyyy HH:mm} and should be replaced.") })
                .SetName("Certificate expires in 14 days - fails.");
            
            yield return new TestCaseData(fifteenDaysFromToday).Returns(new List<EvaluationError>())
                .SetName("Certificate expires in 15 days - succeeds.");

            yield return new TestCaseData(thirtyDaysFromToday).Returns(new List<EvaluationError>())
                .SetName("Certificate expires in 30 days - succeeds.");

            yield return new TestCaseData(multipleExpiredDate).Returns(new List<EvaluationError>
                {
                    new EvaluationError(EvaluationErrorType.Error, $"The certificate Certificate1 expired on {multipleExpiredDate.Certificates[0].ValidTo:dd/MM/yyyy HH:mm} and should be replaced."),
                    new EvaluationError(EvaluationErrorType.Error, $"The certificate Certificate2 expired on {multipleExpiredDate.Certificates[1].ValidTo:dd/MM/yyyy HH:mm} and should be replaced.")
                })
                .SetName("Multiple Certificates expired 1 day ago - fails.");

            yield return new TestCaseData(multipleSevenDaysFromToday).Returns(new List<EvaluationError>
                {
                    new EvaluationError(EvaluationErrorType.Warning, $"The certificate Certificate1 will expire on {multipleSevenDaysFromToday.Certificates[0].ValidTo:dd/MM/yyyy HH:mm} and should be replaced."),
                    new EvaluationError(EvaluationErrorType.Warning, $"The certificate Certificate2 will expire on {multipleSevenDaysFromToday.Certificates[1].ValidTo:dd/MM/yyyy HH:mm} and should be replaced.")
                })
                .SetName("Multiple Certificates expires in 7 days - fails.");

            yield return new TestCaseData(mixOfExpiredAndExpiring).Returns(new List<EvaluationError>
                {
                    new EvaluationError(EvaluationErrorType.Error, $"The certificate Certificate1 expired on {mixOfExpiredAndExpiring.Certificates[0].ValidTo:dd/MM/yyyy HH:mm} and should be replaced."),
                    new EvaluationError(EvaluationErrorType.Warning, $"The certificate Certificate2 will expire on {mixOfExpiredAndExpiring.Certificates[1].ValidTo:dd/MM/yyyy HH:mm} and should be replaced.")
                })
                .SetName("Mixed Certificates expired and expires in 7 days - fails.");
        }

        private static X509Certificate Create(string commonName, DateTime date)
        {
            X509Certificate certificate = A.Fake<X509Certificate>();
            A.CallTo(() => certificate.CommonName).Returns(commonName);
            A.CallTo(() => certificate.ValidTo).Returns(date);
            return certificate;
        }

        private static HostCertificates Create(params X509Certificate[] certificates) => new HostCertificates("host", false, new List<X509Certificate>(certificates), new List<SelectedCipherSuite>());
    }
}
