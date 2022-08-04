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
    public class Tls13AvailableTests
    {
        private Tls13Available _sut;

        [SetUp]
        public void SetUp()
        {
            _sut = new Tls13Available();
        }

        [TestCase(TlsError.TCP_CONNECTION_FAILED, EvaluatorResult.INFORMATIONAL, "This server does not support TLS 1.3", TestName = "Tcp connection failed results in inconclusive")]
        [TestCase(TlsError.SESSION_INITIALIZATION_FAILED, EvaluatorResult.INFORMATIONAL, "This server does not support TLS 1.3", TestName = "Session initialization failed results in inconclusive")]
        [TestCase(TlsError.HANDSHAKE_FAILURE, EvaluatorResult.INFORMATIONAL, "This server does not support TLS 1.3", TestName = "Session initialization failed results in inconclusive")]
        public async Task Test(TlsError? tlsError, EvaluatorResult expectedEvaluatorResult, string expectedDescription)
        {
            BouncyCastleTlsTestResult tlsConnectionResult =
                new BouncyCastleTlsTestResult(null, null, null, null, tlsError, null, new List<string>());

            TlsTestResults connectionTestResults = TlsTestDataUtil.CreateMxHostTlsResults(
                TlsTestType.Tls13AvailableWithBestCipherSuiteSelected, tlsConnectionResult);

            List<RuleTypedTlsEvaluationResult> ruleTypedTlsEvaluationResults =
                await _sut.Evaluate(connectionTestResults);

            Assert.That(ruleTypedTlsEvaluationResults.Count, Is.EqualTo(1));
            Assert.That(ruleTypedTlsEvaluationResults[0].TlsEvaluatedResult.Result, Is.EqualTo(expectedEvaluatorResult));
            StringAssert.StartsWith(expectedDescription, ruleTypedTlsEvaluationResults[0].TlsEvaluatedResult.Description);
        }

        [Test(Description = "No failure results in success")]
        public async Task NoErrorReturnsPass()
        {
            BouncyCastleTlsTestResult tls13ConnectionResult = new BouncyCastleTlsTestResult(null, null, null, null, null, null, new List<string>());

            TlsTestResults connectionTestResults = TlsTestDataUtil.CreateMxHostTlsResults(new Dictionary<TlsTestType, BouncyCastleTlsTestResult>
            {
                {TlsTestType.Tls13AvailableWithBestCipherSuiteSelected, tls13ConnectionResult},
            });

            List<RuleTypedTlsEvaluationResult> ruleTypedTlsEvaluationResults =
                await _sut.Evaluate(connectionTestResults);

            Assert.That(ruleTypedTlsEvaluationResults.Count, Is.EqualTo(1));
            Assert.That(ruleTypedTlsEvaluationResults[0].TlsEvaluatedResult.Result, Is.EqualTo(EvaluatorResult.PASS));
            Assert.That(ruleTypedTlsEvaluationResults[0].TlsEvaluatedResult.Description, Is.Null);
        }
    }
}
