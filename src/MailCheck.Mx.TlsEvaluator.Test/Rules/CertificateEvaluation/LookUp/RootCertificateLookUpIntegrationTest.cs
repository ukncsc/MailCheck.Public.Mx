using System;
using System.Threading.Tasks;
using FakeItEasy;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.LookUp;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using X509Certificate = MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Domain.X509Certificate;

namespace MailCheck.Mx.TlsEvaluator.Test.Rules.CertificateEvaluation.LookUp
{
    /// <summary>
    /// https://ccadb-public.secure.force.com/mozilla/IncludedCACertificateReport
    /// </summary>
    [TestFixture(Category = "Integration")]
    public class RootCertificateLookUpIntegrationTest
    {
        private RootCertificateLookUp _rootCertificateLookUp;
        private IRootCertificateProvider _rootCertificateProvider;
        private IClock _clock;

        [SetUp]
        public void SetUp()
        {
            _clock = A.Fake<IClock>();
            _rootCertificateProvider = new MozillaRootCertificateProvider(A.Fake<ILogger<MozillaRootCertificateProvider>>());
            _rootCertificateLookUp = new RootCertificateLookUp(_rootCertificateProvider, _clock, A.Fake<ILogger<RootCertificateLookUp>>());
        }

        [Test]
        public async Task GetCertificate_ShouldLoadCertsAndReturnOne()
        {
            A.CallTo(() => _clock.GetDateTimeUtc()).Returns(new DateTime(2018, 01, 01));
            string issuer = "CN=Chambers of Commerce Root, OU=http://www.chambersign.org, O=AC Camerfirma SA CIF A82743287, C=EU";

            X509Certificate certificate = await _rootCertificateLookUp.GetCertificate(issuer);

            Assert.That(certificate, Is.Not.Null);
        }
    }
}
