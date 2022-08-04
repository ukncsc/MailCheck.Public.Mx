using System.Collections.Generic;
using System.Linq;
using FakeItEasy;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Domain;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Rules;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace MailCheck.Mx.TlsEvaluator.Test.Rules.CertificateEvaluation.Rules
{
    [TestFixture]
    public class LeafCertificateMustHaveCorrectKeyUsageTests
    {
        LeafCertificateMustHaveCorrectKeyUsage sut;

        [SetUp]
        public void SetUp()
        {
            sut = new LeafCertificateMustHaveCorrectKeyUsage(A.Fake<ILogger<LeafCertificateMustHaveCorrectKeyUsage>>());
        }

        [Test]
        public void ItShouldHaveNoErrorsIfTheKeyUsageIsCorrect()
        {
            var result = sut.Evaluate(CreateHostCertificates("ncsc.gov.uk", CreateCertificates(
                CreateCertificate("cert", true, true, true, true)), CreateSelectedCiphers()));

            Assert.AreEqual(0, result.Result.Count);
        }

        [Test]
        public void ItShouldHaveNoErrorsIfThereAreNoRelevantCiphers()
        {
            var result = sut.Evaluate(CreateHostCertificates("ncsc.gov.uk", CreateCertificates(
                CreateCertificate("cert")), CreateSelectedCiphers()));

            Assert.AreEqual(0, result.Result.Count);
        }

        [Test]
        public void ItShouldHaveAnErrorForANonNullCurveOrGroupWithNoDigitalSignatureBit()
        {
            var result = sut.Evaluate(CreateHostCertificates("ncsc.gov.uk", CreateCertificates(
                CreateCertificate("cert", true)), CreateSelectedCiphers(
                    new SelectedCipherSuite("TlsSecureDiffieHellmanGroupSelected", "TLS_DH_RSA_WITH_AES_128_GCM_SHA256"))));

            Assert.AreEqual(1, result.Result.Count);
        }

        [Test]
        public void ItShouldHaveAnErrorForAnRsaCipherWithNoKeyEncipherment()
        {
            var result = sut.Evaluate(CreateHostCertificates("ncsc.gov.uk", CreateCertificates(
                CreateCertificate("cert", true)), CreateSelectedCiphers(
                    new SelectedCipherSuite("Tls11AvailableWithBestCipherSuiteSelected", "TLS_RSA_WITH_DES_CBC_SHA"))));

            Assert.AreEqual(1, result.Result.Count);
        }

        [Test]
        public void ItShouldHaveAnErrorForADiffieCipherWithNoKeyAgreement()
        {
            var result = sut.Evaluate(CreateHostCertificates("ncsc.gov.uk", CreateCertificates(
                CreateCertificate("cert", true)), CreateSelectedCiphers(
                    new SelectedCipherSuite("Tls12AvailableWithSha2HashFunctionSelected", "TLS_DH_RSA_WITH_AES_128_GCM_SHA256"))));

            Assert.AreEqual(1, result.Result.Count);
        }

        [Test]
        public void ItShouldIgnoreTestsIfCipherSuiteNotAvailable()
        {

            List<SelectedCipherSuite> cipherSuites = new List<SelectedCipherSuite>
            {
                new SelectedCipherSuite("Tls11AvailableWithBestCipherSuiteSelected", null)
            };

            var result = sut.Evaluate(CreateHostCertificates("ncsc.gov.uk", CreateCertificates(
                CreateCertificate("cert", true, true)), cipherSuites));

            Assert.AreEqual(0, result.Result.Count);
        }

        [Test]
        public void ItShouldNotHaveAnErrorForANonNullCurveOrGroupWithADigitalSignatureBit()
        {
            var result = sut.Evaluate(CreateHostCertificates("ncsc.gov.uk", CreateCertificates(
                CreateCertificate("cert", true, true)), CreateSelectedCiphers(
                    new SelectedCipherSuite("TlsSecureEllipticCurveSelected", "TLS_DH_RSA_WITH_AES_128_GCM_SHA256"))));

            Assert.AreEqual(0, result.Result.Count);
        }

        [Test]
        public void ItShouldNotHaveAnErrorForAnRsaCipherWithKeyEncipherment()
        {
            var result = sut.Evaluate(CreateHostCertificates("ncsc.gov.uk", CreateCertificates(
                CreateCertificate("cert", true, false, false, true)), CreateSelectedCiphers(
                    new SelectedCipherSuite("Tls10AvailableWithBestCipherSuiteSelected", "TLS_RSA_WITH_SEED_CBC_SHA"))));

            Assert.AreEqual(0, result.Result.Count);
        }

        [Test]
        public void ItShouldNotHaveAnErrorForADiffieCipherWithKeyAgreement()
        {
            var result = sut.Evaluate(CreateHostCertificates("ncsc.gov.uk", CreateCertificates(
                CreateCertificate("cert", true, false, true)), CreateSelectedCiphers(
                    new SelectedCipherSuite("Tls11AvailableWithBestCipherSuiteSelected", "TLS_DH_RSA_WITH_AES_256_GCM_SHA384"))));

            Assert.AreEqual(0, result.Result.Count);
        }

        [Test]
        public void ItShouldHaveMultipleErrorForADiffieCipherWithKeyAgreement()
        {
            var result = sut.Evaluate(CreateHostCertificates("ncsc.gov.uk", CreateCertificates(
                CreateCertificate("cert", true)), CreateSelectedCiphers(
                    new SelectedCipherSuite("TlsSecureEllipticCurveSelected", "TLS_RSA_WITH_AES_128_GCM_SHA256"),
                    new SelectedCipherSuite("Tls11AvailableWithBestCipherSuiteSelected", "TLS_RSA_WITH_IDEA_CBC_SHA"),
                    new SelectedCipherSuite("Tls10AvailableWithBestCipherSuiteSelected", "TLS_DH_DSS_WITH_3DES_EDE_CBC_SHA"))));

            Assert.AreEqual(3, result.Result.Count);
        }

        [Test]
        public void ItShouldNotHaveAnyErrorsIfTheCertificatesHaveNoKeyUsage()
        {
            var result = sut.Evaluate(CreateHostCertificates("ncsc.gov.uk", CreateCertificates(
                CreateCertificate("cert", false)), CreateSelectedCiphers(
                    new SelectedCipherSuite("TlsSecureEllipticCurveSelected", "TLS_RSA_WITH_AES_128_GCM_SHA256"),
                    new SelectedCipherSuite("Tls11AvailableWithBestCipherSuiteSelected", "TLS_RSA_WITH_IDEA_CBC_SHA"))));

            Assert.AreEqual(0, result.Result.Count);
        }

        private static X509Certificate CreateCertificate(string commonName, bool hasKeyUsage = false,
            bool digitalSignature = false, bool keyAgreement = false, bool keyEncipherment = false)
        {
            X509Certificate certificate = A.Fake<X509Certificate>();

            A.CallTo(() => certificate.HasKeyUsage).Returns(hasKeyUsage);
            A.CallTo(() => certificate.CommonName).Returns(commonName);
            A.CallTo(() => certificate.KeyUsageIncludesDigitalSignature).Returns(digitalSignature);
            A.CallTo(() => certificate.KeyUsageIncludesKeyAgreement).Returns(keyAgreement);
            A.CallTo(() => certificate.KeyUsageIncludesKeyEncipherment).Returns(keyEncipherment);

            return certificate;
        }

        private static HostCertificates CreateHostCertificates(string host, List<X509Certificate> certificates, List<SelectedCipherSuite> ciphers) =>
            new HostCertificates(host, false, certificates, ciphers);

        private static List<X509Certificate> CreateCertificates(params X509Certificate[] certificates) =>
            certificates.ToList();

        private static List<SelectedCipherSuite> CreateSelectedCiphers(params SelectedCipherSuite[] ciphers) =>
            ciphers.ToList();
    }
}
