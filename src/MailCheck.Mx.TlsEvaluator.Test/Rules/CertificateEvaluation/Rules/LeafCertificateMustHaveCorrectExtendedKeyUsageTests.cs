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
    public class LeafCertificateMustHaveCorrectExtendedKeyUsageTests
    {
        private IRule<HostCertificates> sut;

        [SetUp]
        public void SetUp()
        {
            sut = new LeafCertificateMustHaveCorrectExtendedKeyUsage(A.Fake<ILogger<LeafCertificateMustHaveCorrectExtendedKeyUsage>>());
        }

        [Test]
        public void ItShouldHaveNoErrorsForNoExtendedUseLeafCertificate()
        {
            var hostCertificates = CreateHostCertificates("ncsc.gov.uk", CreateCertificate("cert1"));

            Assert.AreEqual(0, sut.Evaluate(hostCertificates).Result.Count);
        }

        [Test]
        public void ItShouldHaveNoErrorsForExtendedUseLeafCertificateWithIdKpServerAuth()
        {
            var hostCertificates = CreateHostCertificates("ncsc.gov.uk", CreateCertificate("cert1", true, true));

            Assert.AreEqual(0, sut.Evaluate(hostCertificates).Result.Count);
        }

        [Test]
        public void ItShouldHaveNoErrorsForExtendedUseLeafCertificateWithAnyExtendedUse()
        {
            var hostCertificates = CreateHostCertificates("ncsc.gov.uk", CreateCertificate("cert1", true, false, true));

            Assert.AreEqual(0, sut.Evaluate(hostCertificates).Result.Count);
        }

        [Test]
        public void ItShouldHaveAnErrorForExtendedUseLeafCertificateWithoutIdKpServerAuthOrAnyExtendedUse()
        {
            var hostCertificates = CreateHostCertificates("ncsc.gov.uk", CreateCertificate("cert1", true));

            Assert.AreEqual(1, sut.Evaluate(hostCertificates).Result.Count);
        }

        [Test]
        public void ItShouldHaveNoErrorsWhenThereAreNoCertificates()
        {
            var hostCertificates = CreateHostCertificates("ncsc.gov.uk");

            Assert.AreEqual(0, sut.Evaluate(hostCertificates).Result.Count);
        }

        private static X509Certificate CreateCertificate(string commonName, bool extendedKeyUsage = false, bool idKpServerAuth = false, bool anyExtendedKeyUsage = false)
        {
            X509Certificate certificate = A.Fake<X509Certificate>();

            A.CallTo(() => certificate.CommonName).Returns(commonName);
            A.CallTo(() => certificate.HasExtendedKeyUsage).Returns(extendedKeyUsage);
            A.CallTo(() => certificate.ExtendedKeyUsageIncludesIdKpServerAuth).Returns(idKpServerAuth);
            A.CallTo(() => certificate.ExtendedKeyUsageIncludesAnyExtendedKeyUsage).Returns(anyExtendedKeyUsage);

            return certificate;
        }

        private static HostCertificates CreateHostCertificates(string host, params X509Certificate[] certificates) =>
            new HostCertificates(host, false, certificates.ToList(), new List<SelectedCipherSuite>());

    }
}
