using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Domain;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Revocation;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Rules;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace MailCheck.Mx.TlsEvaluator.Test.Rules.CertificateEvaluation.Rules
{
    [TestFixture]
    public class NonRootCertificatesShouldNotAppearOnRevocationListsTests
    {
        private NonRootCertificatesShouldNotAppearOnRevocationLists _rule;
        private IOcspValidator _ocspValidator;
        private ICrlValidator _crlValidator;

        [SetUp]
        public void SetUp()
        {
            _ocspValidator = A.Fake<IOcspValidator>();
            _crlValidator = A.Fake<ICrlValidator>();
            _rule = new NonRootCertificatesShouldNotAppearOnRevocationLists(_ocspValidator, _crlValidator,
                A.Fake<ILogger<NonRootCertificatesShouldNotAppearOnRevocationLists>>());
        }

        [Test]
        public async Task AllCertificateValidNoErrorsReturn()
        {
            HostCertificates hostCertificate = Create(
                Create("Leaf"),
                Create("Inter"),
                Create("Root"));

            List<EvaluationError> evaluationErrors = await _rule.Evaluate(hostCertificate);

            A.CallTo(() => _ocspValidator.CheckOcspRevocation(null, null, null)).WithAnyArguments().MustHaveHappenedTwiceExactly();
            A.CallTo(() => _crlValidator.CheckCrlRevocation(null, null)).WithAnyArguments().MustNotHaveHappened();

            Assert.That(evaluationErrors, Is.Empty);
        }

        [Test]
        public async Task CertificateOnOcspRevocationListErrorReturned()
        {
            HostCertificates hostCertificate = Create(
                Create("Leaf"),
                Create("Inter"),
                Create("Root"));

            A.CallTo(() => _ocspValidator.CheckOcspRevocation(null, null, null))
                .WithAnyArguments()
                .ReturnsNextFromSequence(
                    new RevocationResult(true, new List<RevocationInfo>()),
                    new RevocationResult(false, new List<RevocationInfo>()));

            List<EvaluationError> evaluationErrors = await _rule.Evaluate(hostCertificate);

            A.CallTo(() => _ocspValidator.CheckOcspRevocation(null, null, null)).WithAnyArguments().MustHaveHappenedOnceExactly();
            A.CallTo(() => _crlValidator.CheckCrlRevocation(null, null)).WithAnyArguments().MustNotHaveHappened();

            Assert.That(evaluationErrors.Count, Is.EqualTo(1));
            Assert.That(evaluationErrors.First().ErrorType, Is.EqualTo(EvaluationErrorType.Error));
            Assert.That(evaluationErrors.First().Message.StartsWith("The certificate Inter is revoked:"), Is.True);
        }

        [Test]
        public async Task MultipleCertificatesOnOcspRevocationListOnlyFirstOneProcessed()
        {
            HostCertificates hostCertificate = Create(
                Create("Leaf"),
                Create("Inter"),
                Create("Root"));

            A.CallTo(() => _ocspValidator.CheckOcspRevocation(null, null, null))
                .WithAnyArguments()
                .ReturnsNextFromSequence(
                    new RevocationResult(true, new List<RevocationInfo>()),
                    new RevocationResult(true, new List<RevocationInfo>()));

            List<EvaluationError> evaluationErrors = await _rule.Evaluate(hostCertificate);

            A.CallTo(() => _ocspValidator.CheckOcspRevocation(null, null, null)).WithAnyArguments().MustHaveHappenedOnceExactly();
            A.CallTo(() => _crlValidator.CheckCrlRevocation(null, null)).WithAnyArguments().MustNotHaveHappened();

            Assert.That(evaluationErrors.Count, Is.EqualTo(1));
            Assert.That(evaluationErrors.First().ErrorType, Is.EqualTo(EvaluationErrorType.Error));
            Assert.That(evaluationErrors.First().Message.StartsWith("The certificate Inter is revoked:"), Is.True);
        }

        [Test]
        public async Task CrlRevocationAllCertificatesValidReturnsNoErrors()
        {
            HostCertificates hostCertificate = Create(
                Create("Leaf"),
                Create("Inter"),
                Create("Root"));

            A.CallTo(() => _ocspValidator.CheckOcspRevocation(null, null, null))
                .WithAnyArguments()
                .ReturnsNextFromSequence(
                    new RevocationResult("Errored"),
                    new RevocationResult("Errored"));

            List<EvaluationError> evaluationErrors = await _rule.Evaluate(hostCertificate);

            A.CallTo(() => _ocspValidator.CheckOcspRevocation(null, null, null)).WithAnyArguments().MustHaveHappenedTwiceExactly();
            A.CallTo(() => _crlValidator.CheckCrlRevocation(null, null)).WithAnyArguments().MustHaveHappenedTwiceExactly();

            Assert.That(evaluationErrors, Is.Empty);
        }

        [Test]
        public async Task CrlRevocationCertificatesOnRevocationListReturnsErrors()
        {
            HostCertificates hostCertificate = Create(
                Create("Leaf"),
                Create("Inter"),
                Create("Root"));

            A.CallTo(() => _ocspValidator.CheckOcspRevocation(null, null, null))
                .WithAnyArguments()
                .ReturnsNextFromSequence(
                    new RevocationResult("Errored"),
                    new RevocationResult("Errored"));

            A.CallTo(() => _crlValidator.CheckCrlRevocation(null, null))
                .WithAnyArguments()
                .ReturnsNextFromSequence(
                    new RevocationResult(true, new List<RevocationInfo>()),
                    new RevocationResult(false, new List<RevocationInfo>()));

            List<EvaluationError> evaluationErrors = await _rule.Evaluate(hostCertificate);

            A.CallTo(() => _ocspValidator.CheckOcspRevocation(null, null, null)).WithAnyArguments().MustHaveHappenedOnceExactly();
            A.CallTo(() => _crlValidator.CheckCrlRevocation(null, null)).WithAnyArguments().MustHaveHappenedOnceExactly();

            Assert.That(evaluationErrors.Count, Is.EqualTo(1));
            Assert.That(evaluationErrors.First().ErrorType, Is.EqualTo(EvaluationErrorType.Error));
            Assert.That(evaluationErrors.First().Message.StartsWith("The certificate Inter is revoked:"), Is.True);
        }

        [Test]
        public async Task CrlRevocationMultipleCertificatesOnRevocationOnlyFirstOneProcessed()
        {
            HostCertificates hostCertificate = Create(
                Create("Leaf"),
                Create("Inter"),
                Create("Root"));

            A.CallTo(() => _ocspValidator.CheckOcspRevocation(null, null, null))
                .WithAnyArguments()
                .ReturnsNextFromSequence(
                    new RevocationResult("Errored"),
                    new RevocationResult("Errored"));

            A.CallTo(() => _crlValidator.CheckCrlRevocation(null, null))
                .WithAnyArguments()
                .ReturnsNextFromSequence(
                    new RevocationResult(true, new List<RevocationInfo>()),
                    new RevocationResult(true, new List<RevocationInfo>()));

            List<EvaluationError> evaluationErrors = await _rule.Evaluate(hostCertificate);

            A.CallTo(() => _ocspValidator.CheckOcspRevocation(null, null, null)).WithAnyArguments().MustHaveHappenedOnceExactly();
            A.CallTo(() => _crlValidator.CheckCrlRevocation(null, null)).WithAnyArguments().MustHaveHappenedOnceExactly();

            Assert.That(evaluationErrors.Count, Is.EqualTo(1));
            Assert.That(evaluationErrors.First().ErrorType, Is.EqualTo(EvaluationErrorType.Error));
            Assert.That(evaluationErrors.First().Message.StartsWith("The certificate Inter is revoked:"), Is.True);
        }

        [Test]
        public async Task CrlAndOcspErrorErrorReturned()
        {
            HostCertificates hostCertificate = Create(
                Create("Leaf"),
                Create("Inter"),
                Create("Root"));

            A.CallTo(() => _ocspValidator.CheckOcspRevocation(null, null, null))
                .WithAnyArguments()
                .ReturnsNextFromSequence(
                    new RevocationResult("Errored Ocsp"),
                    new RevocationResult(true, new List<RevocationInfo>()));

            A.CallTo(() => _crlValidator.CheckCrlRevocation(null, null))
                .WithAnyArguments()
                .ReturnsNextFromSequence(
                    new RevocationResult("Errored Crl"),
                    new RevocationResult(true, new List<RevocationInfo>()));

            List<EvaluationError> evaluationErrors = await _rule.Evaluate(hostCertificate);

            A.CallTo(() => _ocspValidator.CheckOcspRevocation(null, null, null)).WithAnyArguments().MustHaveHappenedOnceExactly();
            A.CallTo(() => _crlValidator.CheckCrlRevocation(null, null)).WithAnyArguments().MustHaveHappenedOnceExactly();

            Assert.That(evaluationErrors.Count, Is.EqualTo(1));
            Assert.That(evaluationErrors.First().ErrorType, Is.EqualTo(EvaluationErrorType.Inconclusive));
            Assert.That(evaluationErrors.First().Message.StartsWith("The certificate Inter has unknown revocation status."), Is.True);
            Assert.That(evaluationErrors.First().Message.Contains("Errored Ocsp"), Is.True);
            Assert.That(evaluationErrors.First().Message.Contains("Errored Crl"), Is.True);
        }

        [Test]
        public async Task CrlAndOcspErrorErrorReturnedOnlyFirstErrorReported()
        {
            HostCertificates hostCertificate = Create(
                Create("Leaf"),
                Create("Inter"),
                Create("Root"));

            A.CallTo(() => _ocspValidator.CheckOcspRevocation(null, null, null))
                .WithAnyArguments()
                .ReturnsNextFromSequence(
                    new RevocationResult("Errored Ocsp"),
                    new RevocationResult("Errored Ocsp"));

            A.CallTo(() => _crlValidator.CheckCrlRevocation(null, null))
                .WithAnyArguments()
                .ReturnsNextFromSequence(
                    new RevocationResult("Errored Crl"),
                    new RevocationResult("Errored Crl"));

            List<EvaluationError> evaluationErrors = await _rule.Evaluate(hostCertificate);

            A.CallTo(() => _ocspValidator.CheckOcspRevocation(null, null, null)).WithAnyArguments().MustHaveHappenedOnceExactly();
            A.CallTo(() => _crlValidator.CheckCrlRevocation(null, null)).WithAnyArguments().MustHaveHappenedOnceExactly();

            Assert.That(evaluationErrors.Count, Is.EqualTo(1));
            Assert.That(evaluationErrors.First().ErrorType, Is.EqualTo(EvaluationErrorType.Inconclusive));
            Assert.That(evaluationErrors.First().Message.StartsWith("The certificate Inter has unknown revocation status."), Is.True);
            Assert.That(evaluationErrors.First().Message.Contains("Errored Ocsp"), Is.True);
            Assert.That(evaluationErrors.First().Message.Contains("Errored Crl"), Is.True);
        }

        private static X509Certificate Create(string commonName)
        {
            X509Certificate certificate = A.Fake<X509Certificate>();
            A.CallTo(() => certificate.CommonName).Returns(commonName);
            return certificate;
        }

        private static HostCertificates Create(params X509Certificate[] certificates) => new HostCertificates("host", false, new List<X509Certificate>(certificates), new List<SelectedCipherSuite>());
    }
}
