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
    public class Tls12AvailableTests
    {
        [TestCase(TlsError.TCP_CONNECTION_FAILED, EvaluatorResult.INCONCLUSIVE, "When testing TLS 1.2 we were unable to create a connection", TestName = "Tcp connection failed results in inconclusive")]
        [TestCase(TlsError.SESSION_INITIALIZATION_FAILED, EvaluatorResult.INCONCLUSIVE, "When testing TLS 1.2 we were unable to create a connection", TestName = "Session initialization failed results in inconclusive")]
        public async Task Test(TlsError? tlsError, EvaluatorResult expectedEvaluatorResult, string expectedDescription)
        {
            Tls12Available tls12Available = new Tls12Available();

            BouncyCastleTlsTestResult tlsConnectionResult =
                new BouncyCastleTlsTestResult(null, null, null, null, tlsError, null, new List<string>());

            TlsTestResults connectionTestResults = TlsTestDataUtil.CreateMxHostTlsResults(
                TlsTestType.Tls12AvailableWithBestCipherSuiteSelected, tlsConnectionResult);

            List<RuleTypedTlsEvaluationResult> ruleTypedTlsEvaluationResults =
                await tls12Available.Evaluate(connectionTestResults);

            Assert.That(ruleTypedTlsEvaluationResults.Count, Is.EqualTo(1));
            Assert.That(ruleTypedTlsEvaluationResults[0].TlsEvaluatedResult.Result, Is.EqualTo(expectedEvaluatorResult));


            StringAssert.StartsWith(expectedDescription, ruleTypedTlsEvaluationResults[0].TlsEvaluatedResult.Description);
        }
        [Test]
        public async Task Tls12ErrorAndNoOtherTlsSupportedReturnsError()
        {
            Tls12Available tls11Available = new Tls12Available();

            BouncyCastleTlsTestResult tls12ConnectionResult = new BouncyCastleTlsTestResult(null, null, null, null, TlsError.HANDSHAKE_FAILURE, null, new List<string>());

            TlsTestResults connectionTestResults = TlsTestDataUtil.CreateMxHostTlsResults(new Dictionary<TlsTestType, BouncyCastleTlsTestResult>
            {
                {TlsTestType.Tls12AvailableWithBestCipherSuiteSelected, tls12ConnectionResult}
            });

            List<RuleTypedTlsEvaluationResult> ruleTypedTlsEvaluationResults =
                await tls11Available.Evaluate(connectionTestResults);

            Assert.That(ruleTypedTlsEvaluationResults.Count, Is.EqualTo(1));
            Assert.That(ruleTypedTlsEvaluationResults[0].TlsEvaluatedResult.Result, Is.EqualTo(EvaluatorResult.FAIL));
            StringAssert.StartsWith("This server refused to negotiate using TLS 1.2", ruleTypedTlsEvaluationResults[0].TlsEvaluatedResult.Description);
        }

        [Test]
        public async Task NoErrorReturnsPass()
        {
            Tls12Available tls12Available = new Tls12Available();

            BouncyCastleTlsTestResult tls12ConnectionResult = new BouncyCastleTlsTestResult(null, null, null, null, null, null, new List<string>());

            TlsTestResults connectionTestResults = TlsTestDataUtil.CreateMxHostTlsResults(new Dictionary<TlsTestType, BouncyCastleTlsTestResult>
            {
                {TlsTestType.Tls12AvailableWithBestCipherSuiteSelected, tls12ConnectionResult},
            });

            List<RuleTypedTlsEvaluationResult> ruleTypedTlsEvaluationResults =
                await tls12Available.Evaluate(connectionTestResults);

            Assert.That(ruleTypedTlsEvaluationResults.Count, Is.EqualTo(1));
            Assert.That(ruleTypedTlsEvaluationResults[0].TlsEvaluatedResult.Result, Is.EqualTo(EvaluatorResult.PASS));
            Assert.That(ruleTypedTlsEvaluationResults[0].TlsEvaluatedResult.Description, Is.Null);
        }
    }
}
