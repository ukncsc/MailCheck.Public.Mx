using System.Collections.Generic;
using System.Threading.Tasks;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Tester;
using MailCheck.Mx.TlsEvaluator.Rules.TlsEvaluation;
using MailCheck.Mx.TlsEvaluator.Rules.TlsEvaluation.Tls11;
using MailCheck.Mx.TlsEvaluator.Util;
using NUnit.Framework;

namespace MailCheck.Mx.TlsEvaluator.Test.Rules.TlsEvaluation.Tls11
{
    [TestFixture]
    public class Tls11AvailableWithBestCipherSuiteSelectedTest
    {
        private Tls11AvailableWithBestCipherSuiteSelected _sut;

        [SetUp]
        public void SetUp()
        {
            _sut = new Tls11AvailableWithBestCipherSuiteSelected();
        }

        [Test]
        [TestCase(TlsError.TCP_CONNECTION_FAILED)]
        [TestCase(TlsError.SESSION_INITIALIZATION_FAILED)]
        public async Task TcpErrorsShouldResultInInconclusive(TlsError tlsError)
        {
            BouncyCastleTlsTestResult tlsConnectionResult = new BouncyCastleTlsTestResult(tlsError, null, null);
            TlsTestResults connectionTestResults = TlsTestDataUtil.CreateMxHostTlsResults(TlsTestType.Tls11AvailableWithBestCipherSuiteSelected,
                tlsConnectionResult);

            List<RuleTypedTlsEvaluationResult> evaluatorResults = await _sut.Evaluate(connectionTestResults);

            Assert.That(evaluatorResults.Count, Is.EqualTo(1));

            Assert.AreEqual(evaluatorResults[0].TlsEvaluatedResult.Result, EvaluatorResult.INCONCLUSIVE);
        }

        [Test]
        public async Task UnaccountedForCipherSuiteResponseShouldResultInInconclusive()
        {
            BouncyCastleTlsTestResult tlsConnectionResult = new BouncyCastleTlsTestResult(null, CipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_256_CBC_SHA, null, null, null, null, null, null);
            TlsTestResults connectionTestResults = TlsTestDataUtil.CreateMxHostTlsResults(TlsTestType.Tls11AvailableWithBestCipherSuiteSelected,
                tlsConnectionResult);

            List<RuleTypedTlsEvaluationResult> evaluatorResults = await _sut.Evaluate(connectionTestResults);

            Assert.That(evaluatorResults.Count, Is.EqualTo(1));

            Assert.AreEqual(evaluatorResults[0].TlsEvaluatedResult.Result, EvaluatorResult.INCONCLUSIVE);
        }

        [Test]
        [TestCase(CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA)]
        [TestCase(CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA)]
        [TestCase(CipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA)]
        [TestCase(CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA)]
        [TestCase(CipherSuite.TLS_DHE_RSA_WITH_AES_256_CBC_SHA)]
        [TestCase(CipherSuite.TLS_DHE_RSA_WITH_AES_128_CBC_SHA)]
        public async Task GoodCipherSuitesShouldResultInAPass(CipherSuite cipherSuite)
        {
            BouncyCastleTlsTestResult tlsConnectionResult = new BouncyCastleTlsTestResult(null, cipherSuite, null, null, null, null, null, null);
            TlsTestResults connectionTestResults = TlsTestDataUtil.CreateMxHostTlsResults(TlsTestType.Tls11AvailableWithBestCipherSuiteSelected,
                tlsConnectionResult);

            List<RuleTypedTlsEvaluationResult> evaluatorResults = await _sut.Evaluate(connectionTestResults);

            Assert.That(evaluatorResults.Count, Is.EqualTo(1));

            Assert.AreEqual(evaluatorResults[0].TlsEvaluatedResult.Result, EvaluatorResult.PASS);
            Assert.AreEqual(evaluatorResults[0].TlsEvaluatedResult.Description, "TLS 1.1 is available and a secure cipher suite was selected.");
        }

        [Test]
        [TestCase(CipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA)]
        [TestCase(CipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA)]
        [TestCase(CipherSuite.TLS_RSA_WITH_3DES_EDE_CBC_SHA)]
        [TestCase(CipherSuite.TLS_RSA_WITH_RC4_128_SHA)]
        [TestCase(CipherSuite.TLS_DH_DSS_WITH_3DES_EDE_CBC_SHA)]
        [TestCase(CipherSuite.TLS_DH_RSA_WITH_3DES_EDE_CBC_SHA)]
        public async Task CipherSuitesWithNoPfsShouldResultInAWarning(CipherSuite cipherSuite)
        {
            BouncyCastleTlsTestResult tlsConnectionResult = new BouncyCastleTlsTestResult(null, cipherSuite, null, null, null, null, null, null);
            TlsTestResults connectionTestResults = TlsTestDataUtil.CreateMxHostTlsResults(TlsTestType.Tls11AvailableWithBestCipherSuiteSelected,
                tlsConnectionResult);

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
        public async Task InsecureCipherSuitesShouldResultInAFail(CipherSuite cipherSuite)
        {
            BouncyCastleTlsTestResult tlsConnectionResult = new BouncyCastleTlsTestResult(null, cipherSuite, null, null, null, null, null, null);
            TlsTestResults connectionTestResults = TlsTestDataUtil.CreateMxHostTlsResults(TlsTestType.Tls11AvailableWithBestCipherSuiteSelected,
                tlsConnectionResult);

            List<RuleTypedTlsEvaluationResult> evaluatorResults = await _sut.Evaluate(connectionTestResults);

            Assert.That(evaluatorResults.Count, Is.EqualTo(1));

            Assert.AreEqual(evaluatorResults[0].TlsEvaluatedResult.Result, EvaluatorResult.FAIL);
        }
    }
}
