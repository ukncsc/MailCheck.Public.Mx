using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FakeItEasy;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.LookUp;
using NUnit.Framework;
using X509Certificate = MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Domain.X509Certificate;

namespace MailCheck.Mx.TlsEvaluator.Test.Rules.CertificateEvaluation.LookUp
{
    [TestFixture]
    public class RootCertificateLookUpTest
    {
        private RootCertificateLookUp _rootCertificateLookUp;
        private IRootCertificateProvider _rootCertificateProvider;
        private IClock _clock;

        [SetUp]
        public void SetUp()
        {
            _rootCertificateProvider = A.Fake<IRootCertificateProvider>();
            _clock = A.Fake<IClock>();
            _rootCertificateLookUp = new RootCertificateLookUp(_rootCertificateProvider, _clock);
        }

        [Test]
        public async Task GetCertificatesGoesToOriginForStateOnFirstCallAndReturnsValue()
        {
            A.CallTo(() => _clock.GetDateTimeUtc()).Returns(new DateTime(2018, 01, 01));
            string issuer = "CN=ABC, O=ABC, S=LONDON, C=uk";

            X509Certificate x509Certificate = A.Fake<X509Certificate>();
            A.CallTo(() => x509Certificate.Issuer).Returns(issuer);
            A.CallTo(() => x509Certificate.Subject).Returns(issuer);
            
            A.CallTo(() => _rootCertificateProvider.GetRootCaCertificates())
                .Returns(Task.FromResult(new List<X509Certificate>{x509Certificate}));

            X509Certificate certificate = await _rootCertificateLookUp.GetCertificate(issuer);

            Assert.That(certificate, Is.Not.Null);
            A.CallTo(() => _rootCertificateProvider.GetRootCaCertificates()).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task GetCertificatesDoesntGoToSourceForStateOnSecondCallAndReturnsValue()
        {
            A.CallTo(() => _clock.GetDateTimeUtc()).Returns(new DateTime(2018, 01, 01));
            string issuer = "CN=ABC, O=ABC, S=LONDON, C=uk";

            X509Certificate x509Certificate = A.Fake<X509Certificate>();
            A.CallTo(() => x509Certificate.Issuer).Returns(issuer);
            A.CallTo(() => x509Certificate.Subject).Returns(issuer);

            A.CallTo(() => _rootCertificateProvider.GetRootCaCertificates())
                .Returns(Task.FromResult(new List<X509Certificate> { x509Certificate }));

            X509Certificate certificate1 = await _rootCertificateLookUp.GetCertificate(issuer);
            X509Certificate certificate2 = await _rootCertificateLookUp.GetCertificate(issuer);

            Assert.That(certificate1, Is.Not.Null);
            Assert.That(certificate1, Is.SameAs(certificate2));
            A.CallTo(() => _rootCertificateProvider.GetRootCaCertificates()).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task GetCertificatesCertificateDoesntExistReturnsNull()
        {
            A.CallTo(() => _clock.GetDateTimeUtc()).Returns(new DateTime(2018, 01, 01));
            string issuer = "CN=ABC, O=ABC, S=LONDON, C=uk";
            string issuer1 = "CN=ABC, O=ABC, S=NEWYORK, C=us";

            X509Certificate x509Certificate = A.Fake<X509Certificate>();
            A.CallTo(() => x509Certificate.Issuer).Returns(issuer);
            A.CallTo(() => x509Certificate.Subject).Returns(issuer);

            A.CallTo(() => _rootCertificateProvider.GetRootCaCertificates())
                .Returns(Task.FromResult(new List<X509Certificate> { x509Certificate }));

            X509Certificate certificate = await _rootCertificateLookUp.GetCertificate(issuer1);

            Assert.That(certificate, Is.Null);
            A.CallTo(() => _rootCertificateProvider.GetRootCaCertificates()).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task GetCertificateGoToSourceForStateAfterTimeoutAndReturnsValue()
        {
            A.CallTo(() => _clock.GetDateTimeUtc()).Returns(new DateTime(2018, 01, 01));
            string issuer = "CN=ABC, O=ABC, S=LONDON, C=uk";

            X509Certificate x509Certificate = A.Fake<X509Certificate>();
            A.CallTo(() => x509Certificate.Issuer).Returns(issuer);
            A.CallTo(() => x509Certificate.Subject).Returns(issuer);

            A.CallTo(() => _rootCertificateProvider.GetRootCaCertificates())
                .Returns(Task.FromResult(new List<X509Certificate> { x509Certificate }));

            X509Certificate certificate1 = await _rootCertificateLookUp.GetCertificate(issuer);

            A.CallTo(() => _clock.GetDateTimeUtc()).Returns(new DateTime(2018, 01, 01).AddDays(7));

            X509Certificate certificate2 = await _rootCertificateLookUp.GetCertificate(issuer);

            Assert.That(certificate1, Is.Not.Null);
            Assert.That(certificate1, Is.SameAs(certificate2));
            A.CallTo(() => _rootCertificateProvider.GetRootCaCertificates()).MustHaveHappenedTwiceExactly();
        }
    }
}
