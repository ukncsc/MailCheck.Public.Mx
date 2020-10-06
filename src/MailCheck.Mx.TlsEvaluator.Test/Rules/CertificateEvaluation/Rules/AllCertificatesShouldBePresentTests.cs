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
    public class AllCertificatesShouldBePresentTests
    {
        private AllCertificatesShouldBePresent _allCertificatesShouldBePresent;

        [SetUp]
        public void SetUp()
        {
            _allCertificatesShouldBePresent = new AllCertificatesShouldBePresent(A.Fake<ILogger<AllCertificatesShouldBePresent>>());
        }

        [Test]
        public async Task ItShouldPassWhenAllIssuersArePresent()
        {
            HostCertificates hostCertificates = Create(
                Create(
                    "C=BE, O=GlobalSign nv-sa, CN=GlobalSign Organization Validation CA - SHA256 - G2",
                    "C=US, ST=California, L=San Francisco, O=Wikimedia Foundation, Inc., CN=*.wikipedia.org"),
                Create(
                    "C=BE, O=GlobalSign nv-sa, CN=GlobalSign Organization Validation CA - SHA256 - G2",
                    "C=BE, O=GlobalSign nv-sa, CN=GlobalSign Organization Validation CA - SHA256 - G2"));

            List<EvaluationError> errors = await _allCertificatesShouldBePresent.Evaluate(hostCertificates);

            Assert.That(errors, Is.Empty);
        }

        [Test]
        public async Task WhiteSpaceShouldntMatter()
        {
            HostCertificates hostCertificates = Create(
                Create(
                    "  C=BE, O=GlobalSign nv-sa, CN=GlobalSign Organization Validation CA - SHA256 - G2  ",
                    "C=US, ST=California, L=San Francisco, O=Wikimedia Foundation, Inc., CN=*.wikipedia.org"),
                Create(
                    "C=BE, O=GlobalSign nv-sa, CN=GlobalSign Organization Validation CA - SHA256 - G2",
                    "C=BE, O=GlobalSign nv-sa, CN=GlobalSign Organization Validation CA - SHA256 - G2"));

            List<EvaluationError> errors = await _allCertificatesShouldBePresent.Evaluate(hostCertificates);

            Assert.That(errors, Is.Empty);
        }

        [Test]
        public async Task ItShouldPassEvenIfTheOrderIsIncorrect()
        {
            HostCertificates hostCertificates = Create(
                Create(
                    "C=BE, O=GlobalSign nv-sa, CN=GlobalSign Organization Validation CA - SHA256 - G2",
                    "C=US, ST=California, L=San Francisco, O=Wikimedia Foundation, Inc., CN=*.wikipedia.org"),
                Create(
                    "C=BE, O=GlobalSign nv-sa, CN=GlobalSign Organization Validation CA - SHA256 - G2",
                    "C=BE, O=GlobalSign nv-sa, CN=GlobalSign Organization Validation CA - SHA256 - G2"));

            List<EvaluationError> errors = await _allCertificatesShouldBePresent.Evaluate(hostCertificates);

            Assert.That(errors, Is.Empty);
        }

        [Test]
        public async Task ItShouldFailWhenAllIssuersAreNotPresent()
        {
            HostCertificates hostCertificates = Create(
                Create(
                    "C=BE, O=GlobalSign nv-sa, CN=GlobalSign Organization Validation CA - SHA256 - G2",
                    "C=US, ST=California, L=San Francisco, O=Wikimedia Foundation, Inc., CN=*.wikipedia.org"),
                Create(
                    "CN = ABC, O = ABC, S = LONDON, C = uk",
                    "CN = DEF, O = DEF, S = LONDON, C = uk"));

            List<EvaluationError> errors = await _allCertificatesShouldBePresent.Evaluate(hostCertificates);

            Assert.That(errors.Count, Is.EqualTo(2));
            Assert.That(errors[0].ErrorType, Is.EqualTo(EvaluationErrorType.Error));
            Assert.That(errors[0].Message, Is.EqualTo("The certificate chain is missing the certificate with issuer C=BE, O=GlobalSign nv-sa, CN=GlobalSign Organization Validation CA - SHA256 - G2."));
            Assert.That(errors[1].ErrorType, Is.EqualTo(EvaluationErrorType.Error));
            Assert.That(errors[1].Message, Is.EqualTo("The certificate chain is missing the certificate with issuer CN = ABC, O = ABC, S = LONDON, C = uk."));
        }

        private X509Certificate Create(string issuer, string subject)
        {
            X509Certificate certificate = A.Fake<X509Certificate>();
            A.CallTo(() => certificate.Issuer).Returns(issuer);
            A.CallTo(() => certificate.Subject).Returns(subject);
            return certificate;
        }

        private HostCertificates Create(params X509Certificate[] certificates) => new HostCertificates("host", false, new List<X509Certificate>(certificates), new List<SelectedCipherSuite>());
    }
}
