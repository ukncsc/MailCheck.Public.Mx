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
    public class Tls12AvailableWithSha2HashFunctionSelectedTest
    {
        private Tls12AvailableWithSha2HashFunctionSelected _sut;

        [SetUp]
        public void SetUp()
        {
            _sut = new Tls12AvailableWithSha2HashFunctionSelected();
        }

        [Test]
        [TestCase(TlsError.TCP_CONNECTION_FAILED)]
        [TestCase(TlsError.SESSION_INITIALIZATION_FAILED)]
        public async Task TcpErrorsShouldResultInInconclusive(TlsError tlsError)
        {
            BouncyCastleTlsTestResult tlsConnectionResult = new BouncyCastleTlsTestResult(tlsError, null, null);
            TlsTestResults connectionTestResults = TlsTestDataUtil.CreateMxHostTlsResults(TlsTestType.Tls12AvailableWithSha2HashFunctionSelected,
                tlsConnectionResult);

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
                TlsTestDataUtil.CreateMxHostTlsResults(TlsTestType.Tls12AvailableWithSha2HashFunctionSelected, tlsConnectionResult);

            List<RuleTypedTlsEvaluationResult> evaluatorResults = await _sut.Evaluate(connectionTestResults);

            Assert.That(evaluatorResults.Count, Is.EqualTo(1));

            Assert.AreEqual(evaluatorResults[0].TlsEvaluatedResult.Result, EvaluatorResult.INCONCLUSIVE);
            StringAssert.Contains($"Error description \"{description}\".", evaluatorResults[0].TlsEvaluatedResult.Description);
        }

        [Test]
        public async Task AnErrorShouldResultInAFail()
        {
            BouncyCastleTlsTestResult tlsConnectionResult = new BouncyCastleTlsTestResult(TlsError.INSUFFICIENT_SECURITY, null, null);
            TlsTestResults connectionTestResults = TlsTestDataUtil.CreateMxHostTlsResults(TlsTestType.Tls12AvailableWithSha2HashFunctionSelected,
                tlsConnectionResult);

            List<RuleTypedTlsEvaluationResult> evaluatorResults = await _sut.Evaluate(connectionTestResults);

            Assert.That(evaluatorResults.Count, Is.EqualTo(1));

            Assert.AreEqual(evaluatorResults[0].TlsEvaluatedResult.Result, EvaluatorResult.FAIL);
        }

        [Test]
        public async Task UnaccountedForCipherSuiteResponseShouldResultInInconclusive()
        {
            BouncyCastleTlsTestResult tlsConnectionResult = new BouncyCastleTlsTestResult(null, CipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_256_CBC_SHA, null, null, null, null, null, null);
            TlsTestResults connectionTestResults = TlsTestDataUtil.CreateMxHostTlsResults(TlsTestType.Tls12AvailableWithSha2HashFunctionSelected,
                tlsConnectionResult);

            List<RuleTypedTlsEvaluationResult> evaluatorResults = await _sut.Evaluate(connectionTestResults);

            Assert.That(evaluatorResults.Count, Is.EqualTo(1));

            Assert.AreEqual(evaluatorResults[0].TlsEvaluatedResult.Result, EvaluatorResult.INCONCLUSIVE);
        }

        [Test]
        [TestCase(CipherSuite.TLS_RSA_WITH_AES_256_GCM_SHA384)]
        [TestCase(CipherSuite.TLS_RSA_WITH_AES_128_GCM_SHA256)]
        [TestCase(CipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA256)]
        [TestCase(CipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA256)]
        public async Task GoodCiphersShouldResultInAPass(CipherSuite cipherSuite)
        {
            BouncyCastleTlsTestResult tlsConnectionResult = new BouncyCastleTlsTestResult(null, cipherSuite, null, null, null, null, null, null);
            TlsTestResults connectionTestResults = TlsTestDataUtil.CreateMxHostTlsResults(TlsTestType.Tls12AvailableWithSha2HashFunctionSelected,
                tlsConnectionResult);

            List<RuleTypedTlsEvaluationResult> evaluatorResults = await _sut.Evaluate(connectionTestResults);

            Assert.That(evaluatorResults.Count, Is.EqualTo(1));

            Assert.AreEqual(evaluatorResults[0].TlsEvaluatedResult.Result, EvaluatorResult.PASS);
        }

        [Test]
        [TestCase(CipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA)]
        [TestCase(CipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA)]
        public async Task CipherSuitesThatUseSha1ShouldResultInAWarning(CipherSuite cipherSuite)
        {
            BouncyCastleTlsTestResult tlsConnectionResult = new BouncyCastleTlsTestResult(null, cipherSuite, null, null, null, null, null, null);
            TlsTestResults connectionTestResults = TlsTestDataUtil.CreateMxHostTlsResults(TlsTestType.Tls12AvailableWithSha2HashFunctionSelected,
                tlsConnectionResult);

            List<RuleTypedTlsEvaluationResult> evaluatorResults = await _sut.Evaluate(connectionTestResults);

            Assert.That(evaluatorResults.Count, Is.EqualTo(1));

            Assert.AreEqual(evaluatorResults[0].TlsEvaluatedResult.Result, EvaluatorResult.WARNING);
        }

        [Test]
        [TestCase(CipherSuite.TLS_DH_DSS_WITH_3DES_EDE_CBC_SHA)]
        [TestCase(CipherSuite.TLS_DH_RSA_WITH_3DES_EDE_CBC_SHA)]
        public async Task CipherSuitesThatUseSha1AndHaveNoPfsShouldResultInAWarning(CipherSuite cipherSuite)
        {
            BouncyCastleTlsTestResult tlsConnectionResult = new BouncyCastleTlsTestResult(null, cipherSuite, null, null, null, null, null, null);
            TlsTestResults connectionTestResults = TlsTestDataUtil.CreateMxHostTlsResults(TlsTestType.Tls12AvailableWithSha2HashFunctionSelected,
                tlsConnectionResult);

            List<RuleTypedTlsEvaluationResult> evaluatorResults = await _sut.Evaluate(connectionTestResults);

            Assert.That(evaluatorResults.Count, Is.EqualTo(1));

            Assert.AreEqual(evaluatorResults[0].TlsEvaluatedResult.Result, EvaluatorResult.WARNING);
        }

        [Test]
        [TestCase(CipherSuite.TLS_RSA_WITH_3DES_EDE_CBC_SHA)]
        [TestCase(CipherSuite.TLS_DHE_DSS_WITH_3DES_EDE_CBC_SHA)]
        public async Task CipherSuitesThatUse3DesShouldResultInAWarning(CipherSuite cipherSuite)
        {
            BouncyCastleTlsTestResult tlsConnectionResult = new BouncyCastleTlsTestResult(null, cipherSuite, null, null, null, null, null, null);
            TlsTestResults connectionTestResults = TlsTestDataUtil.CreateMxHostTlsResults(TlsTestType.Tls12AvailableWithSha2HashFunctionSelected,
                tlsConnectionResult);

            List<RuleTypedTlsEvaluationResult> evaluatorResults = await _sut.Evaluate(connectionTestResults);

            Assert.That(evaluatorResults.Count, Is.EqualTo(1));

            Assert.AreEqual(evaluatorResults[0].TlsEvaluatedResult.Result, EvaluatorResult.WARNING);
        }

        [Test]
        [TestCase(CipherSuite.TLS_RSA_WITH_RC4_128_SHA)]
        public async Task CipherSuitesThatUseRc4ShouldResultInAWarning(CipherSuite cipherSuite)
        {
            BouncyCastleTlsTestResult tlsConnectionResult = new BouncyCastleTlsTestResult(null, cipherSuite, null, null, null, null, null, null);
            TlsTestResults connectionTestResults = TlsTestDataUtil.CreateMxHostTlsResults(TlsTestType.Tls12AvailableWithSha2HashFunctionSelected,
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
        [TestCase(CipherSuite.TLS_DHE_RSA_WITH_DES_CBC_SHA)]
        public async Task InsecureCipherSuitesShouldResultInAFail(CipherSuite cipherSuite)
        {
            BouncyCastleTlsTestResult tlsConnectionResult = new BouncyCastleTlsTestResult(null, cipherSuite, null, null, null, null, null, null);
            TlsTestResults connectionTestResults = TlsTestDataUtil.CreateMxHostTlsResults(TlsTestType.Tls12AvailableWithSha2HashFunctionSelected,
                tlsConnectionResult);

            List<RuleTypedTlsEvaluationResult> evaluatorResults = await _sut.Evaluate(connectionTestResults);

            Assert.That(evaluatorResults.Count, Is.EqualTo(1));

            Assert.AreEqual(evaluatorResults[0].TlsEvaluatedResult.Result, EvaluatorResult.FAIL);
        }
    }
}
