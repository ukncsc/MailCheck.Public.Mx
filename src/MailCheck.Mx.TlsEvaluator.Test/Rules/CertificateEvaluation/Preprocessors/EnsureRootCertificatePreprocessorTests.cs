using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Domain;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.LookUp;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Preprocessors;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace MailCheck.Mx.TlsEvaluator.Test.Rules.CertificateEvaluation.Preprocessors
{
    [TestFixture]
    public class EnsureRootCertificatePreprocessorTests
    {
        private IRootCertificateLookUp _rootCertificateLookUp;
        private EnsureRootCertificatePreprocessor _ensureRootCertificatePreprocessor;

        [SetUp]
        public void SetUp()
        {
            _rootCertificateLookUp = A.Fake<IRootCertificateLookUp>();

            _ensureRootCertificatePreprocessor = new EnsureRootCertificatePreprocessor(_rootCertificateLookUp,
                A.Fake<ILogger<EnsureRootCertificatePreprocessor>>());
        }

        [Test]
        public async Task NoCertificatesNoChangesToChain()
        {
            HostCertificates originalHostCertificates = new HostCertificates("host", false, new List<X509Certificate>(), new List<SelectedCipherSuite>());
            HostCertificates updatedHostCertificates = await _ensureRootCertificatePreprocessor.Preprocess(originalHostCertificates);

            Assert.That(updatedHostCertificates.Certificates.SequenceEqual(originalHostCertificates.Certificates));
        }

        [Test]
        public async Task RootCertificateInChainNoChangesToChain()
        {
            HostCertificates originalHostCertificates = Create(
                Create(
                    "C=BE, O=GlobalSign nv-sa, CN=GlobalSign Organization Validation CA - SHA256 - G2",
                    "C=US, ST=California, L=San Francisco, O=Wikimedia Foundation, Inc., CN=*.wikipedia.org"),
                Create(
                    "C=BE, O=GlobalSign nv-sa, CN=GlobalSign Organization Validation CA - SHA256 - G2",
                    "C=BE, O=GlobalSign nv-sa, CN=GlobalSign Organization Validation CA - SHA256 - G2"));

            HostCertificates updatedHostCertificates = await _ensureRootCertificatePreprocessor.Preprocess(originalHostCertificates);

            Assert.That(updatedHostCertificates.Certificates.SequenceEqual(originalHostCertificates.Certificates));
        }

        [Test]
        public async Task RootCertificateInWrongPlaceInChainNoChangesToChain()
        {
            HostCertificates originalHostCertificates = Create(
                Create(
                    "C=BE, O=GlobalSign nv-sa, CN=GlobalSign Organization Validation CA - SHA256 - G2",
                    "C=BE, O=GlobalSign nv-sa, CN=GlobalSign Organization Validation CA - SHA256 - G2"),
                Create(
                    "C=BE, O=GlobalSign nv-sa, CN=GlobalSign Organization Validation CA - SHA256 - G2",
                    "C=US, ST=California, L=San Francisco, O=Wikimedia Foundation, Inc., CN=*.wikipedia.org")
                );

            HostCertificates updatedHostCertificates = await _ensureRootCertificatePreprocessor.Preprocess(originalHostCertificates);

            Assert.That(updatedHostCertificates.Certificates.SequenceEqual(originalHostCertificates.Certificates));
        }

        [Test]
        public async Task NoRootCertifcateAndRootCertificateDoenstExistsNoChangeToChain()
        {
            HostCertificates originalHostCertificates = Create(
                Create(
                    "CN=DigiCert Global CA G2, O=DigiCert Inc, C=US",
                    "CN=*.mimecast.com, OU=Technical Operations, O=Mimecast Services Limited, L=London, C=GB"),
                Create(
                    "CN=DigiCert Global Root G2, OU=www.digicert.com, O=DigiCert Inc, C=US",
                    "CN=DigiCert Global CA G2, O=DigiCert Inc, C=US"));

            HostCertificates updatedHostCertificates = await _ensureRootCertificatePreprocessor.Preprocess(originalHostCertificates);

            Assert.That(updatedHostCertificates.Certificates.SequenceEqual(originalHostCertificates.Certificates));
        }

        [Test]
        public async Task NoRootCertifcateAndRootCertificateExistsAppendsRootCertifcateToChain()
        {
            HostCertificates originalHostCertificates = Create(
                Create(
                    "CN=DigiCert Global CA G2, O=DigiCert Inc, C=US",
                    "CN=*.mimecast.com, OU=Technical Operations, O=Mimecast Services Limited, L=London, C=GB"),
                Create(
                    "CN=DigiCert Global Root G2, OU=www.digicert.com, O=DigiCert Inc, C=US",
                    "CN=DigiCert Global CA G2, O=DigiCert Inc, C=US"));

            X509Certificate rootCertificate = Create(
                "CN=DigiCert Global Root G2, OU=www.digicert.com, O=DigiCert Inc, C=US",
                "CN=DigiCert Global Root G2, OU=www.digicert.com, O=DigiCert Inc, C=US");

            A.CallTo(() => _rootCertificateLookUp.GetCertificate(rootCertificate.Issuer)).Returns(rootCertificate);

            HostCertificates updatedHostCertificates = await _ensureRootCertificatePreprocessor.Preprocess(originalHostCertificates);

            Assert.That(updatedHostCertificates.Certificates.Count, Is.EqualTo(3));
            Assert.That(updatedHostCertificates.Certificates[0], Is.EqualTo(originalHostCertificates.Certificates[0]));
            Assert.That(updatedHostCertificates.Certificates[1], Is.EqualTo(originalHostCertificates.Certificates[1]));
            Assert.That(updatedHostCertificates.Certificates[2], Is.EqualTo(rootCertificate));
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
