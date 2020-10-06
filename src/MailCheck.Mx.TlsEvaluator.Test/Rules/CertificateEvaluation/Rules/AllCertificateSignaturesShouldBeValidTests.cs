using System;
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
    public class AllCertificateSignaturesShouldBeValidTests
    {
        private AllCertificatesSignaturesShouldBeValid _allSignaturesShouldBeValid;

        [SetUp]
        public void SetUp()
        {
            _allSignaturesShouldBeValid = new AllCertificatesSignaturesShouldBeValid(A.Fake<ILogger<AllCertificatesSignaturesShouldBeValid>>());
        }

        [Test]
        public async Task AllSignaturesValidNoError()
        {
            HostCertificates hostCertificates = Create(Create("Certificate1", true), Create("Certificate2", true));
            List<EvaluationError> errors = await _allSignaturesShouldBeValid.Evaluate(hostCertificates);

            Assert.That(errors, Is.Empty);
        }

        [Test]
        public async Task InvalidSignaturesRaiseErrors()
        {
            HostCertificates hostCertificates = Create(Create("Certificate1", false), Create("Certificate2", true), Create("Certificate3", true));
            List<EvaluationError> errors = await _allSignaturesShouldBeValid.Evaluate(hostCertificates);

            Assert.That(errors.Count, Is.EqualTo(1));
            Assert.That(errors[0].ErrorType, Is.EqualTo(EvaluationErrorType.Error));
            Assert.That(errors[0].Message, Is.EqualTo("The certificate Certificate1 does not have a valid signature."));
        }

        private X509Certificate Create(string commonName, bool signatureValid)
        {
            X509Certificate certificate = A.Fake<X509Certificate>();
            A.CallTo(() => certificate.CommonName).Returns(commonName);
            if (!signatureValid)
            {
                A.CallTo(() => certificate.VerifySignature(null, null)).WithAnyArguments().Throws(new Exception());
            }

            return certificate;
        }

        private HostCertificates Create(params X509Certificate[] certificates) => new HostCertificates("host", false, new List<X509Certificate>(certificates), new List<SelectedCipherSuite>());
    }
}
