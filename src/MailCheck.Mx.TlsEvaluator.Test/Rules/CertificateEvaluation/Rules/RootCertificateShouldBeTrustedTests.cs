using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Domain;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.LookUp;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Rules;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace MailCheck.Mx.TlsEvaluator.Test.Rules.CertificateEvaluation.Rules
{
    [TestFixture]
    public class RootCertificateShouldBeTrustedTests
    {
        private RootCertificateShouldBeTrusted _tlsRootCertificateShouldBeTrusted;
        private IRootCertificateLookUp _rootCertificateLookUp;

        [SetUp]
        public void SetUp()
        {
            _rootCertificateLookUp = A.Fake<IRootCertificateLookUp>();
            _tlsRootCertificateShouldBeTrusted = new RootCertificateShouldBeTrusted(_rootCertificateLookUp, A.Fake<ILogger<RootCertificateShouldBeTrusted>>());
        }

        [Test]
        public async Task RootCertificateInLookUpRootIsTrusted()
        {
            string issuer = "CN=ABC, O=ABC, S=LONDON, C=uk";

            HostCertificates hostCertificates = Create("Certificate1", issuer);

            A.CallTo(() => _rootCertificateLookUp.GetCertificate(issuer)).Returns(hostCertificates.Certificates.First());

            List<EvaluationError> evaluationError = await _tlsRootCertificateShouldBeTrusted.Evaluate(hostCertificates);

            Assert.That(evaluationError, Is.Empty);
        }

        [Test]
        public async Task RootCertificateNotInLookUpRootIsNotTrusted()
        {
            string issuer = "CN=ABC, O=ABC, S=LONDON, C=uk";

            A.CallTo(() => _rootCertificateLookUp.GetCertificate(issuer)).Returns(Task.FromResult<X509Certificate>(null));

            List<EvaluationError> evaluationError = await _tlsRootCertificateShouldBeTrusted.Evaluate(Create("Certificate1", issuer));

            Assert.That(evaluationError.Count, Is.EqualTo(1));
            Assert.That(evaluationError.First().ErrorType, Is.EqualTo(EvaluationErrorType.Error));
            Assert.That(evaluationError.First().Message, Is.EqualTo("The root certificate Certificate1 is not from a trusted certificate authority."));
        }


        private static HostCertificates Create(string commonName, string issuer)
        {
            X509Certificate certificate = A.Fake<X509Certificate>();
            A.CallTo(() => certificate.CommonName).Returns(commonName);
            A.CallTo(() => certificate.Issuer).Returns(issuer);
            return new HostCertificates("hostname", false, new List<X509Certificate> { certificate }, new List<SelectedCipherSuite>());
        }
    }
}
