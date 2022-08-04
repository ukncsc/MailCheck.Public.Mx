using System.Collections.Generic;
using System.Linq;
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
    public class RootAndIntermediateCertificatesMustHaveKeyCertSignTests
    {
        private IRule<HostCertificates> sut;

        [SetUp]
        public void SetUp()
        {
            sut = new RootAndIntermediateCertificatesMustHaveKeyCertSign(A.Fake<ILogger<RootAndIntermediateCertificatesMustHaveKeyCertSign>>());
        }

        [Test]
        public void ItShouldNotContainAnyErrorsIfRootCertificateHasKeyCertSign()
        {
            var hostCertificates = CreateHostCertificates("ncsc.gov.uk", CreateCertificate("leaf", false), CreateCertificate("root", true));

            Assert.AreEqual(0, sut.Evaluate(hostCertificates).Result.Count);
        }

        [Test]
        public void ItShouldNotContainAnyErrorsForLeafOnlyCertificateWithNoKeyCertSign()
        {
            var hostCertificates = CreateHostCertificates("ncsc.gov.uk", CreateCertificate("leaf", false));

            Assert.AreEqual(0, sut.Evaluate(hostCertificates).Result.Count);
        }

        [Test]
        public void ItShouldContainErrorsForARootCertificateWithNoKeyCertSign()
        {
            var hostCertificates = CreateHostCertificates("ncsc.gov.uk", CreateCertificate("leaf", false), CreateCertificate("root", false));

            Assert.AreEqual(1, sut.Evaluate(hostCertificates).Result.Count);
        }

        [Test]
        public void ItShouldContainErrorsForAnyIntermediateCertificatesWithNoKeyCertSign()
        {
            var hostCertificates = CreateHostCertificates("ncsc.gov.uk",
                CreateCertificate("leaf", false),
                CreateCertificate("intermediate1", false),
                CreateCertificate("intermediate2", false),
                CreateCertificate("root", true));

            Assert.AreEqual(2, sut.Evaluate(hostCertificates).Result.Count);
        }

        [Test]
        public void NonRootCertsShouldContainAnInconclusiveMessageForNoKeyUsageExtension()
        {
            var hostCertificates = CreateHostCertificates("ncsc.gov.uk",
                CreateCertificate("leaf", false),
                CreateCertificate("intermediate1", false, false));

            var result = sut.Evaluate(hostCertificates).Result;

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(EvaluationErrorType.Inconclusive, result[0].ErrorType);
        }

        [Test]
        public void RootCertShouldNotContainAnInconclusiveMessageForNoKeyUsageExtension()
        {
            var hostCertificates = CreateHostCertificates("ncsc.gov.uk",
                CreateCertificate("root", true, subject: "Issuer"));

            var result = sut.Evaluate(hostCertificates).Result;

            Assert.AreEqual(0, result.Count);
        }

        private static X509Certificate CreateCertificate(string commonName, bool keyCertSign, bool hasKeyUsage = true, string issuer = "Issuer", string subject = "Subject")
        {
            X509Certificate certificate = A.Fake<X509Certificate>();

            A.CallTo(() => certificate.CommonName).Returns(commonName);
            A.CallTo(() => certificate.KeyUsageIncludesKeyCertSign).Returns(keyCertSign);
            A.CallTo(() => certificate.HasKeyUsage).Returns(hasKeyUsage);
            A.CallTo(() => certificate.Issuer).Returns(issuer);
            A.CallTo(() => certificate.Subject).Returns(subject);

            return certificate;
        }

        private static HostCertificates CreateHostCertificates(string host, params X509Certificate[] certificates) =>
            new HostCertificates(host, false, certificates.ToList(), new List<SelectedCipherSuite>());
    }
}
