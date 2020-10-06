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
    public class AllCertificatesShouldHaveStrongKeyTests
    {
        [TestCaseSource(nameof(RsaTestData))]
        [TestCaseSource(nameof(EccTestData))]
        public async Task<List<EvaluationError>> Test(string key, int keyLength, HostCertificates hostCertificates)
        {
            AllCertificatesShouldHaveStrongKey rule = new AllCertificatesShouldHaveStrongKey(A.Fake<ILogger<AllCertificatesShouldHaveStrongKey>>());
            return await rule.Evaluate(hostCertificates);
        }

        public static IEnumerable<TestCaseData> EccTestData()
        {
            var certWithStrongEcKey = Create("Certificate1", "ECC", 356);
            var certWithStrongEcKey2 = Create("Certificate2", "ECC", 356);
            var certWithWeakEcKey = Create("Certificate3", "ECC", 126);
            var certWithWeakEcKey2 = Create("Certificate4", "ECC", 10);

            yield return new TestCaseData("ECC", 256, Create(certWithStrongEcKey, certWithWeakEcKey)).Returns(new List<EvaluationError> {
                    new EvaluationError(EvaluationErrorType.Error, "The certificate Certificate3 has a weak ECC key (126 bits). ECC keys should be at least 256 bits.")})
                .SetName("Strong and weak ecc key for certificate - fails.");

            yield return new TestCaseData("ECC", 256, Create(certWithStrongEcKey, certWithStrongEcKey2))
                .Returns(new List<EvaluationError>())
                .SetName("All Certificates have strong ecc key - succeeds.");

            yield return new TestCaseData("ECC", 256, Create(
                    certWithWeakEcKey,
                    certWithWeakEcKey2,
                    certWithStrongEcKey,
                    certWithStrongEcKey2)).Returns(new List<EvaluationError> {
                        new EvaluationError(EvaluationErrorType.Error, "The certificate Certificate3 has a weak ECC key (126 bits). ECC keys should be at least 256 bits."),
                        new EvaluationError(EvaluationErrorType.Error, "The certificate Certificate4 has a weak ECC key (10 bits). ECC keys should be at least 256 bits.")
                    })
                .SetName("Multi strong and weak ecc key for certificate - fails.");
        }

        public static IEnumerable<TestCaseData> RsaTestData()
        {
            var certWithStrongRsaKey = Create("Certificate1", "RSA", 3048);
            var certWithStrongRsaKey2 = Create("Certificate2", "RSA", 2048);
            var certWithWeakRsaKey = Create("Certificate3", "RSA", 1026);
            var certWithWeakRsaKey2 = Create("Certificate4", "RSA", 100);

            yield return new TestCaseData("RSA", 2048, Create(certWithStrongRsaKey, certWithWeakRsaKey)).Returns(new List<EvaluationError> {
                    new EvaluationError(EvaluationErrorType.Error, "The certificate Certificate3 has a weak RSA key (1026 bits). RSA keys should be at least 2048 bits.")})
                .SetName("Strong and weak rsa key for certificate - fails.");

            yield return new TestCaseData("RSA", 2048,
                Create(certWithStrongRsaKey, certWithStrongRsaKey2))
                .Returns(new List<EvaluationError>())
                .SetName("All Certificates have strong rsa key - succeeds.");

            yield return new TestCaseData("RSA", 2048, Create(
                    certWithStrongRsaKey,
                    certWithStrongRsaKey2,
                    certWithWeakRsaKey,
                    certWithWeakRsaKey2
                )).Returns(new List<EvaluationError> {
                    new EvaluationError(EvaluationErrorType.Error, "The certificate Certificate3 has a weak RSA key (1026 bits). RSA keys should be at least 2048 bits."),
                    new EvaluationError(EvaluationErrorType.Error, "The certificate Certificate4 has a weak RSA key (100 bits). RSA keys should be at least 2048 bits.")})
                .SetName("Multiple strong and weak rsa key for certificate - fails.");
        }

        private static X509Certificate Create(string commonName, string key, int keyLength)
        {
            X509Certificate certificate = A.Fake<X509Certificate>();
            A.CallTo(() => certificate.CommonName).Returns(commonName);
            A.CallTo(() => certificate.KeyAlgoritm).Returns(key);
            A.CallTo(() => certificate.KeyLength).Returns(keyLength);
            return certificate;
        }

        private static HostCertificates Create(params X509Certificate[] certificates) => new HostCertificates("host", false, new List<X509Certificate>(certificates), new List<SelectedCipherSuite>());
    }
}
