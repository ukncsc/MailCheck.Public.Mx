using System.Collections.Generic;
using System.Threading.Tasks;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Tester;
using MailCheck.Mx.TlsEvaluator.Rules.TlsEvaluation;
using MailCheck.Mx.TlsEvaluator.Rules.TlsEvaluation.Tls12;
using MailCheck.Mx.TlsEvaluator.Util;
using NUnit.Framework;

namespace MailCheck.Mx.TlsEvaluator.Test.Rules.TlsEvaluation.Tls12
{
    [TestFixture]
    public class TlsWeakCipherSuitesRejectedTest
    {
        private TlsWeakCipherSuitesRejected _sut;

        [SetUp]
        public void SetUp()
        {
            _sut = new TlsWeakCipherSuitesRejected();
        }


        [Test]
        [TestCase(TlsError.TCP_CONNECTION_FAILED)]
        [TestCase(TlsError.SESSION_INITIALIZATION_FAILED)]
        public async Task TcpErrorsShouldResultInInconclusive(TlsError tlsError)
        {
            BouncyCastleTlsTestResult tlsConnectionResult = new BouncyCastleTlsTestResult(tlsError, null, null);
           TlsTestResults connectionTestResults =
                TlsTestDataUtil.CreateMxHostTlsResults(TlsTestType.TlsWeakCipherSuitesRejected, tlsConnectionResult);

            List<RuleTypedTlsEvaluationResult> evaluatorResults = await _sut.Evaluate(connectionTestResults);

            Assert.That(evaluatorResults.Count, Is.EqualTo(1));

            Assert.AreEqual(evaluatorResults[0].TlsEvaluatedResult.Result, EvaluatorResult.INCONCLUSIVE);
        }

        [Test]
        [TestCase(TlsError.TCP_CONNECTION_FAILED,
            "The server did not present a STARTTLS command with a response code (250)")]
        [TestCase(TlsError.SESSION_INITIALIZATION_FAILED,
            "The server did not present a STARTTLS command with a response code (250)")]
        public async Task ErrorsShouldHaveErrorDescriptionInResult(TlsError tlsError, string description)
        {
            BouncyCastleTlsTestResult tlsConnectionResult = new BouncyCastleTlsTestResult(tlsError, description, null);
            TlsTestResults connectionTestResults =
                TlsTestDataUtil.CreateMxHostTlsResults(TlsTestType.TlsWeakCipherSuitesRejected, tlsConnectionResult);

            List<RuleTypedTlsEvaluationResult> evaluatorResults = await _sut.Evaluate(connectionTestResults);

            Assert.That(evaluatorResults.Count, Is.EqualTo(1));

            Assert.AreEqual(evaluatorResults[0].TlsEvaluatedResult.Result, EvaluatorResult.INCONCLUSIVE);
            StringAssert.Contains($"Error description \"{description}\".", evaluatorResults[0].TlsEvaluatedResult.Description);
        }

        [Test]
        [TestCase(TlsError.HANDSHAKE_FAILURE)]
        [TestCase(TlsError.PROTOCOL_VERSION)]
        [TestCase(TlsError.INSUFFICIENT_SECURITY)]
        public async Task ConnectionRefusedErrorsShouldResultInPass(TlsError tlsError)
        {
            BouncyCastleTlsTestResult tlsConnectionResult = new BouncyCastleTlsTestResult(tlsError, null, null);
           TlsTestResults connectionTestResults =
                TlsTestDataUtil.CreateMxHostTlsResults(TlsTestType.TlsWeakCipherSuitesRejected, tlsConnectionResult);

            List<RuleTypedTlsEvaluationResult> evaluatorResults = await _sut.Evaluate(connectionTestResults);

            Assert.That(evaluatorResults.Count, Is.EqualTo(1));

            Assert.AreEqual(evaluatorResults[0].TlsEvaluatedResult.Result, EvaluatorResult.PASS);
        }

        [Test]
        public async Task OtherErrorsShouldResultInInconclusive()
        {
            BouncyCastleTlsTestResult tlsConnectionResult = new BouncyCastleTlsTestResult(TlsError.INTERNAL_ERROR, null, null);
           TlsTestResults connectionTestResults =
                TlsTestDataUtil.CreateMxHostTlsResults(TlsTestType.TlsWeakCipherSuitesRejected, tlsConnectionResult);

            List<RuleTypedTlsEvaluationResult> evaluatorResults = await _sut.Evaluate(connectionTestResults);

            Assert.That(evaluatorResults.Count, Is.EqualTo(1));

            Assert.AreEqual(evaluatorResults[0].TlsEvaluatedResult.Result, EvaluatorResult.INCONCLUSIVE);
        }

        [Test]
        public async Task NoCipherSuiteResponseShouldResultInInconclusive()
        {
            BouncyCastleTlsTestResult tlsConnectionResult = new BouncyCastleTlsTestResult(null, null, null, null, null, null, null, null);
           TlsTestResults connectionTestResults =
                TlsTestDataUtil.CreateMxHostTlsResults(TlsTestType.TlsWeakCipherSuitesRejected, tlsConnectionResult);

            List<RuleTypedTlsEvaluationResult> evaluatorResults = await _sut.Evaluate(connectionTestResults);

            Assert.That(evaluatorResults.Count, Is.EqualTo(1));

            Assert.AreEqual(evaluatorResults[0].TlsEvaluatedResult.Result, EvaluatorResult.INCONCLUSIVE);
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
        public async Task InsecureCipherSuitesShouldResultInAFail(CipherSuite cipherSuite)
        {
            BouncyCastleTlsTestResult tlsConnectionResult = new BouncyCastleTlsTestResult(null, cipherSuite, null, null, null, null, null, null);
           TlsTestResults connectionTestResults =
                TlsTestDataUtil.CreateMxHostTlsResults(TlsTestType.TlsWeakCipherSuitesRejected, tlsConnectionResult);

            List<RuleTypedTlsEvaluationResult> evaluatorResults = await _sut.Evaluate(connectionTestResults);

            Assert.That(evaluatorResults.Count, Is.EqualTo(1));

            Assert.AreEqual(evaluatorResults[0].TlsEvaluatedResult.Result, EvaluatorResult.FAIL);
        }
    }
}
