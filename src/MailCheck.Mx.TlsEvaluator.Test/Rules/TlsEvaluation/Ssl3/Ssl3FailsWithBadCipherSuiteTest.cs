using System.Collections.Generic;
using System.Threading.Tasks;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Tester;
using MailCheck.Mx.TlsEvaluator.Rules.TlsEvaluation;
using MailCheck.Mx.TlsEvaluator.Rules.TlsEvaluation.Ssl3;
using MailCheck.Mx.TlsEvaluator.Util;
using NUnit.Framework;

namespace MailCheck.Mx.TlsEvaluator.Test.Rules.TlsEvaluation.Ssl3
{
    [TestFixture]
    public class Ssl3FailsWithBadCipherSuiteTest
    {
        private Ssl3FailsWithBadCipherSuite _sut;

        [SetUp]
        public void SetUp()
        {
            _sut = new Ssl3FailsWithBadCipherSuite();
        }

        [Test]
        [TestCase(TlsError.TCP_CONNECTION_FAILED,
            "The server did not present a STARTTLS command with a response code (250)")]
        [TestCase(TlsError.SESSION_INITIALIZATION_FAILED,
            "The server did not present a STARTTLS command with a response code (250)")]
        [TestCase(TlsError.BAD_CERTIFICATE,
            "The server returned bad certificate")]
        public async Task ErrorsShouldHaveErrorDescriptionInResult(TlsError tlsError, string description)
        {
            BouncyCastleTlsTestResult BouncyCastleTlsTestResult = new BouncyCastleTlsTestResult(tlsError, description, null);
            TlsTestResults connectionTestResults =
                TlsTestDataUtil.CreateMxHostTlsResults(TlsTestType.Ssl3FailsWithBadCipherSuite, BouncyCastleTlsTestResult);

            List<RuleTypedTlsEvaluationResult> evaluatorResults = await _sut.Evaluate(connectionTestResults);

            Assert.That(evaluatorResults.Count, Is.EqualTo(1));

            Assert.AreEqual(evaluatorResults[0].TlsEvaluatedResult.Result, EvaluatorResult.INCONCLUSIVE);
            StringAssert.Contains($"Error description \"{description}\".", evaluatorResults[0].TlsEvaluatedResult.Description);
        }

        [Test]
        [TestCase(TlsError.HANDSHAKE_FAILURE, "Handshake failure!")]
        [TestCase(TlsError.PROTOCOL_VERSION, "Incorrect Protocol version!")]
        [TestCase(TlsError.INSUFFICIENT_SECURITY, "Insufficient security!")]
        public async Task ConnectionRefusedErrorsShouldResultInPassWithoutErrorDescription(TlsError tlsError, string description)
        {
            BouncyCastleTlsTestResult BouncyCastleTlsTestResult = new BouncyCastleTlsTestResult(tlsError, null, null);
            TlsTestResults connectionTestResults = TlsTestDataUtil.CreateMxHostTlsResults(TlsTestType.Ssl3FailsWithBadCipherSuite, BouncyCastleTlsTestResult);

            List<RuleTypedTlsEvaluationResult> evaluatorResults = await _sut.Evaluate(connectionTestResults);

            Assert.That(evaluatorResults.Count, Is.EqualTo(1));

            Assert.AreEqual(evaluatorResults[0].TlsEvaluatedResult.Result, EvaluatorResult.INFORMATIONAL);
            Assert.That(evaluatorResults[0].TlsEvaluatedResult.Description, Is.Null);
        }

        [Test]
        [TestCase(TlsError.TCP_CONNECTION_FAILED)]
        [TestCase(TlsError.SESSION_INITIALIZATION_FAILED)]
        public async Task TcpErrorsShouldResultInInconclusive(TlsError tlsError)
        {
            string errorDescription = "Something went wrong!";
            BouncyCastleTlsTestResult BouncyCastleTlsTestResult = new BouncyCastleTlsTestResult(tlsError, errorDescription, null);
            TlsTestResults connectionTestResults = TlsTestDataUtil.CreateMxHostTlsResults(TlsTestType.Ssl3FailsWithBadCipherSuite, BouncyCastleTlsTestResult);

            List<RuleTypedTlsEvaluationResult> evaluatorResults = await _sut.Evaluate(connectionTestResults);

            Assert.That(evaluatorResults.Count, Is.EqualTo(1));

            Assert.AreEqual(evaluatorResults[0].TlsEvaluatedResult.Result, EvaluatorResult.INCONCLUSIVE);
            StringAssert.Contains($"Error description \"{errorDescription}\".", evaluatorResults[0].TlsEvaluatedResult.Description);
        }

        [Test]
        [TestCase(TlsError.HANDSHAKE_FAILURE)]
        [TestCase(TlsError.PROTOCOL_VERSION)]
        [TestCase(TlsError.INSUFFICIENT_SECURITY)]
        public async Task ConnectionRefusedErrorsShouldResultInPass(TlsError tlsError)
        {
            BouncyCastleTlsTestResult BouncyCastleTlsTestResult = new BouncyCastleTlsTestResult(tlsError, null, null);
            TlsTestResults connectionTestResults = TlsTestDataUtil.CreateMxHostTlsResults(TlsTestType.Ssl3FailsWithBadCipherSuite, BouncyCastleTlsTestResult);

            var evaluatorResults = await _sut.Evaluate(connectionTestResults);

            Assert.That(evaluatorResults.Count, Is.EqualTo(1));

            Assert.AreEqual(evaluatorResults[0].TlsEvaluatedResult.Result, EvaluatorResult.INFORMATIONAL);
        }

        [Test]
        public async Task OtherErrorsShouldResultInInconclusive()
        {
            string errorDescription = "Something went wrong!";
            BouncyCastleTlsTestResult BouncyCastleTlsTestResult = new BouncyCastleTlsTestResult(TlsError.INTERNAL_ERROR, errorDescription, null);
            TlsTestResults connectionTestResults =TlsTestDataUtil.CreateMxHostTlsResults(TlsTestType.Ssl3FailsWithBadCipherSuite, BouncyCastleTlsTestResult);

            List<RuleTypedTlsEvaluationResult> evaluatorResults = await _sut.Evaluate(connectionTestResults);

            Assert.That(evaluatorResults.Count, Is.EqualTo(1));

            Assert.AreEqual(evaluatorResults[0].TlsEvaluatedResult.Result, EvaluatorResult.INCONCLUSIVE);
            StringAssert.Contains($"Error description \"{errorDescription}\".", evaluatorResults[0].TlsEvaluatedResult.Description);
        }

        [Test]
        public async Task UnaccountedForCipherSuiteResponseShouldResultInInconclusive()
        {
            BouncyCastleTlsTestResult BouncyCastleTlsTestResult = new BouncyCastleTlsTestResult(null, CipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_256_CBC_SHA, null, null, null, null, null);
            TlsTestResults connectionTestResults = TlsTestDataUtil.CreateMxHostTlsResults(TlsTestType.Ssl3FailsWithBadCipherSuite, BouncyCastleTlsTestResult);

            List<RuleTypedTlsEvaluationResult> evaluatorResults = await _sut.Evaluate(connectionTestResults);

            Assert.That(evaluatorResults.Count, Is.EqualTo(1));

            Assert.AreEqual(evaluatorResults[0].TlsEvaluatedResult.Result, EvaluatorResult.INCONCLUSIVE);
        }

        [Test]
        [TestCase(CipherSuite.TLS_RSA_WITH_RC4_128_SHA)]
        [TestCase(CipherSuite.TLS_DH_DSS_WITH_3DES_EDE_CBC_SHA)]
        [TestCase(CipherSuite.TLS_DH_RSA_WITH_3DES_EDE_CBC_SHA)]
        public async Task NoPfsCipherSuiteShouldResultInWarning(CipherSuite cipherSuite)
        {
            BouncyCastleTlsTestResult BouncyCastleTlsTestResult = new BouncyCastleTlsTestResult(null, cipherSuite, null, null, null, null, null);
            TlsTestResults connectionTestResults = TlsTestDataUtil.CreateMxHostTlsResults(TlsTestType.Ssl3FailsWithBadCipherSuite, BouncyCastleTlsTestResult);

            List<RuleTypedTlsEvaluationResult> evaluatorResults = await _sut.Evaluate(connectionTestResults);

            Assert.That(evaluatorResults.Count, Is.EqualTo(1));

            Assert.AreEqual(evaluatorResults[0].TlsEvaluatedResult.Result, EvaluatorResult.WARNING);
        }

        [Test]
        [TestCase(CipherSuite.TLS_RSA_WITH_RC4_128_MD5)]
        [TestCase(CipherSuite.TLS_NULL_WITH_NULL_NULL)]
        [TestCase(CipherSuite.TLS_RSA_WITH_NULL_MD5)]
        [TestCase(CipherSuite.TLS_RSA_WITH_NULL_SHA)]
        [TestCase(CipherSuite.TLS_RSA_EXPORT_WITH_RC4_40_MD5)]
        [TestCase(CipherSuite.TLS_RSA_EXPORT_WITH_RC2_CBC_40_MD5)]
        [TestCase(CipherSuite.TLS_RSA_EXPORT_WITH_DES40_CBC_SHA)]
        [TestCase(CipherSuite.TLS_RSA_WITH_DES_CBC_SHA)]
        [TestCase(CipherSuite.TLS_DH_DSS_EXPORT_WITH_DES40_CBC_SHA)]
        [TestCase(CipherSuite.TLS_DH_DSS_WITH_DES_CBC_SHA)]
        [TestCase(CipherSuite.TLS_DH_RSA_EXPORT_WITH_DES40_CBC_SHA)]
        [TestCase(CipherSuite.TLS_DH_RSA_WITH_DES_CBC_SHA)]
        [TestCase(CipherSuite.TLS_DHE_DSS_EXPORT_WITH_DES40_CBC_SHA)]
        [TestCase(CipherSuite.TLS_DHE_DSS_WITH_DES_CBC_SHA)]
        [TestCase(CipherSuite.TLS_DHE_RSA_EXPORT_WITH_DES40_CBC_SHA)]
        public async Task InsecureCipherSuitesShouldResultInFail(CipherSuite cipherSuite)
        {
            BouncyCastleTlsTestResult BouncyCastleTlsTestResult = new BouncyCastleTlsTestResult(null, cipherSuite, null, null, null, null, null);
            TlsTestResults connectionTestResults = TlsTestDataUtil.CreateMxHostTlsResults(TlsTestType.Ssl3FailsWithBadCipherSuite, BouncyCastleTlsTestResult);

            List<RuleTypedTlsEvaluationResult> evaluatorResults = await _sut.Evaluate(connectionTestResults);

            Assert.That(evaluatorResults.Count, Is.EqualTo(1));

            Assert.AreEqual(evaluatorResults[0].TlsEvaluatedResult.Result, EvaluatorResult.FAIL);
        }
    }
}
