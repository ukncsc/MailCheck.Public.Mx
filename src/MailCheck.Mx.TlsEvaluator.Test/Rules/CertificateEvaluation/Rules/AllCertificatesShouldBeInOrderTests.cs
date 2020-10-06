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
    public class AllCertificatesShouldBeInOrderTests
    {
        private AllCertificatesShouldBeInOrder _allCertificatesShouldBeInOrder;

        [SetUp]
        public void SetUp()
        {
            _allCertificatesShouldBeInOrder = new AllCertificatesShouldBeInOrder(A.Fake<ILogger<AllCertificatesShouldBeInOrder>>());
        }

        [Test]
        public async Task ItShouldPassWhenTheOrderIsCorrect()
        {
            HostCertificates hostCertificates = Create(
                Create(
                    "C=BE, O=GlobalSign nv-sa, CN=GlobalSign Organization Validation CA - SHA256 - G2",
                    "C=US, ST=California, L=San Francisco, O=Wikimedia Foundation, Inc., CN=*.wikipedia.org"),
                Create(
                    "C=BE, O=GlobalSign nv-sa, CN=GlobalSign Organization Validation CA - SHA256 - G2",
                    "C=BE, O=GlobalSign nv-sa, CN=GlobalSign Organization Validation CA - SHA256 - G2"));

            List<EvaluationError> errors = await _allCertificatesShouldBeInOrder.Evaluate(hostCertificates);

            Assert.That(errors, Is.Empty);
        }

        [Test]
        public async Task ItShouldFailWhenTheOrderIsIncorrect()
        {
            HostCertificates hostCertificates = Create(
                Create(
                    "C=BE, O=GlobalSign nv-sa, CN=GlobalSign Organization Validation CA - SHA256 - G2",
                    "C=BE, O=GlobalSign nv-sa, CN=GlobalSign Organization Validation CA - SHA256 - G2"),
                Create(
                    "C=BE, O=GlobalSign nv-sa, CN=GlobalSign Organization Validation CA - SHA256 - G2",
                    "C=US, ST=California, L=San Francisco, O=Wikimedia Foundation, Inc., CN=*.wikipedia.org"));

            List<EvaluationError> errors = await _allCertificatesShouldBeInOrder.Evaluate(hostCertificates);

            Assert.That(errors.Count, Is.EqualTo(1));
            Assert.That(errors.First().ErrorType, Is.EqualTo(EvaluationErrorType.Error));
            Assert.That(errors.First().Message, Is.EqualTo("The certificate chain is not in the correct order. Certificates must be ordered from the mailserver's certificate to the root certificate, with each certificate's issuer directly following it."));
        }

        [Test]
        public async Task ItShouldFailWhenNoRootIssuerIsPresent()
        {
            HostCertificates hostCertificates = Create(
                Create(
                    "C=BE, O=GlobalSign nv-sa, CN=GlobalSign Organization Validation CA - SHA256 - G2",
                    "C=US, ST=California, L=San Francisco, O=Wikimedia Foundation, Inc., CN=*.wikipedia.org"));

            List<EvaluationError> errors = await _allCertificatesShouldBeInOrder.Evaluate(hostCertificates);

            Assert.That(errors.Count, Is.EqualTo(1));
            Assert.That(errors.First().ErrorType, Is.EqualTo(EvaluationErrorType.Error));
            Assert.That(errors.First().Message, Is.EqualTo("The certificate chain is not in the correct order. Certificates must be ordered from the mailserver's certificate to the root certificate, with each certificate's issuer directly following it."));
        }

        [Test]
        public async Task ItShouldNotFailWhenNoCertificatesArePresent()
        {
            List<EvaluationError> errors = await _allCertificatesShouldBeInOrder.Evaluate(new HostCertificates("", false, new List<X509Certificate>(), new List<SelectedCipherSuite>()));
            Assert.That(errors, Is.Empty);
        }

        [Test]
        public async Task ItShouldFailWhenAnIssuerIsNotTheNextSubjectInTheChain()
        {
            HostCertificates hostCertificates = Create(
                Create(
                    "C=flim, O=flam nv-sa, CN=flum CA - SHA256 - G2",
                    "C=US, ST=California, L=San Francisco, O=Wikimedia Foundation, Inc., CN=*.wikipedia.org"),
                Create(
                    "C=flim, O=flam nv-sa, CN=flum CA - SHA256 - G2",
                    "C=BE, O=GlobalSign nv-sa, CN=GlobalSign Organization Validation CA - SHA256 - G2"));

            List<EvaluationError> errors = await _allCertificatesShouldBeInOrder.Evaluate(hostCertificates);

            Assert.That(errors.Count, Is.EqualTo(1));
            Assert.That(errors.First().ErrorType, Is.EqualTo(EvaluationErrorType.Error));
            Assert.That(errors.First().Message, Is.EqualTo("The certificate chain is not in the correct order. Certificates must be ordered from the mailserver's certificate to the root certificate, with each certificate's issuer directly following it."));
        }

        [Test]
        public async Task ItShouldBeCaseInsensitiveAndIgnorePreceedingAndTrailingWhiteSpace()
        {
            HostCertificates hostCertificates = Create(
                Create(
                    "    C=BE, O=GlobalSign nv-sa, CN=gLoBaLsIgn OrGaNiZaTion VaLiDaTiOn ca - SHA256 - G2    ",
                    "C=US, ST=California, L=San Francisco, O=Wikimedia Foundation, Inc., CN=*.wikipedia.org"),
                Create(
                    "C=BE, O=GlobalSign nv-sa, CN=GlobalSign Organization Validation CA - SHA256 - G2",
                    "C=BE, O=GlobalSign nv-sa, CN=GlobalSign Organization Validation CA - SHA256 - G2"));

            List<EvaluationError> errors = await _allCertificatesShouldBeInOrder.Evaluate(hostCertificates);

            Assert.That(errors, Is.Empty);
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
