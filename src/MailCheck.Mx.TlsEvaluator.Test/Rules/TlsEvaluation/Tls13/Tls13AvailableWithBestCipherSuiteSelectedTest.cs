using System.Collections.Generic;
using System.Threading.Tasks;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Tester;
using MailCheck.Mx.TlsEvaluator.Rules.TlsEvaluation;
using MailCheck.Mx.TlsEvaluator.Rules.TlsEvaluation.Tls13;
using MailCheck.Mx.TlsEvaluator.Util;
using NUnit.Framework;

namespace MailCheck.Mx.TlsEvaluator.Test.Rules.TlsEvaluation.Tls13
{
    [TestFixture]
    public class Tls13AvailableWithBestCipherSuiteSelectedTest
    {
        private Tls13AvailableWithBestCipherSuiteSelected _sut;

        [SetUp]
        public void SetUp()
        {
            _sut = new Tls13AvailableWithBestCipherSuiteSelected();
        }

        [Test]
        [TestCase(TlsError.TCP_CONNECTION_FAILED)]
        [TestCase(TlsError.SESSION_INITIALIZATION_FAILED)]
        public async Task TcpErrorsShouldResultInInconclusive(TlsError tlsError)
        {
            BouncyCastleTlsTestResult tlsConnectionResult = new BouncyCastleTlsTestResult(tlsError, null, null);
            TlsTestResults connectionTestResults = TlsTestDataUtil.CreateMxHostTlsResults(TlsTestType.Tls13AvailableWithBestCipherSuiteSelected,
                tlsConnectionResult);

            List<RuleTypedTlsEvaluationResult> evaluatorResults = await _sut.Evaluate(connectionTestResults);

            Assert.That(evaluatorResults.Count, Is.EqualTo(1));

            Assert.AreEqual(EvaluatorResult.INFORMATIONAL, evaluatorResults[0].TlsEvaluatedResult.Result);
            Assert.AreEqual("This mailserver does not support TLS 1.3 with the recommended ciphersuites.", evaluatorResults[0].TlsEvaluatedResult.Description);
        }

        [Test]
        [TestCase(CipherSuite.TLS_AES_256_GCM_SHA384)]
        [TestCase(CipherSuite.TLS_AES_128_GCM_SHA256)]
        public async Task GoodCipherSuitesShouldResultInAPass(CipherSuite cipherSuite)
        {
            BouncyCastleTlsTestResult tlsConnectionResult = new BouncyCastleTlsTestResult(null, cipherSuite, null, null, null, null, null, null);
            TlsTestResults connectionTestResults = TlsTestDataUtil.CreateMxHostTlsResults(TlsTestType.Tls13AvailableWithBestCipherSuiteSelected,
                tlsConnectionResult);

            List<RuleTypedTlsEvaluationResult> evaluatorResults = await _sut.Evaluate(connectionTestResults);

            Assert.That(evaluatorResults.Count, Is.EqualTo(1));

            Assert.AreEqual(EvaluatorResult.PASS, evaluatorResults[0].TlsEvaluatedResult.Result);
            Assert.AreEqual("This mailserver supports TLS 1.3 with recommended ciphersuites.", evaluatorResults[0].TlsEvaluatedResult.Description);
        }

        [TestCase(CipherSuite.TLS_AES_128_CCM_SHA256)]
        [TestCase(CipherSuite.TLS_AES_128_CCM_8_SHA256)]
        [TestCase(CipherSuite.TLS_CHACHA20_POLY1305_SHA256)]
        [TestCase(CipherSuite.TLS_SM4_GCM_SM3)]
        [TestCase(CipherSuite.TLS_SM4_CCM_SM3)]
        public async Task InsecureCipherSuitesShouldResultInAInformational(CipherSuite cipherSuite)
        {
            BouncyCastleTlsTestResult tlsConnectionResult = new BouncyCastleTlsTestResult(null, cipherSuite, null, null, null, null, null, null);
            TlsTestResults connectionTestResults = TlsTestDataUtil.CreateMxHostTlsResults(TlsTestType.Tls13AvailableWithBestCipherSuiteSelected,
                tlsConnectionResult);

            List<RuleTypedTlsEvaluationResult> evaluatorResults = await _sut.Evaluate(connectionTestResults);

            Assert.That(evaluatorResults.Count, Is.EqualTo(1));

            Assert.AreEqual(EvaluatorResult.INFORMATIONAL, evaluatorResults[0].TlsEvaluatedResult.Result);
            Assert.AreEqual("This mailserver does not support TLS 1.3 with the recommended ciphersuites.", evaluatorResults[0].TlsEvaluatedResult.Description);
        }
    }
}
