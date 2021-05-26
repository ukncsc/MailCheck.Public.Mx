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
    public class Tls11AvailableTests
    {
        [TestCase(TlsError.TCP_CONNECTION_FAILED, EvaluatorResult.INCONCLUSIVE, "When testing TLS 1.1 we were unable to create a connection", TestName = "Tcp connection failed results in inconclusive")]
        [TestCase(TlsError.SESSION_INITIALIZATION_FAILED, EvaluatorResult.INCONCLUSIVE, "When testing TLS 1.1 we were unable to create a connection", TestName = "Session initialization failed results in inconclusive")]
        public async Task Test(TlsError? tlsError, EvaluatorResult expectedEvaluatorResult, string expectedDescription)
        {
            Tls11Available tls11Available = new Tls11Available();

            BouncyCastleTlsTestResult tlsConnectionResult =
                new BouncyCastleTlsTestResult(null, null, null, null, tlsError, null, new List<string>());

            TlsTestResults connectionTestResults = TlsTestDataUtil.CreateMxHostTlsResults(
                TlsTestType.Tls11AvailableWithBestCipherSuiteSelected, tlsConnectionResult);

            List<RuleTypedTlsEvaluationResult> ruleTypedTlsEvaluationResults =
                await tls11Available.Evaluate(connectionTestResults);

            Assert.That(ruleTypedTlsEvaluationResults.Count, Is.EqualTo(1));
            Assert.That(ruleTypedTlsEvaluationResults[0].TlsEvaluatedResult.Result, Is.EqualTo(expectedEvaluatorResult));
            StringAssert.StartsWith(expectedDescription, ruleTypedTlsEvaluationResults[0].TlsEvaluatedResult.Description);
        }

        [Test]
        public async Task Tls11ErrorAndNoOtherTlsSupportedReturnsError()
        {
            Tls11Available tls11Available = new Tls11Available();

            BouncyCastleTlsTestResult tls11ConnectionResult = new BouncyCastleTlsTestResult(null, null, null, null, TlsError.HANDSHAKE_FAILURE, null, new List<string>());

            TlsTestResults connectionTestResults = TlsTestDataUtil.CreateMxHostTlsResults(new Dictionary<TlsTestType, BouncyCastleTlsTestResult>
            {
                {TlsTestType.Tls11AvailableWithBestCipherSuiteSelected, tls11ConnectionResult}
            });

            List<RuleTypedTlsEvaluationResult> ruleTypedTlsEvaluationResults =
                await tls11Available.Evaluate(connectionTestResults);

            Assert.That(ruleTypedTlsEvaluationResults.Count, Is.EqualTo(1));
            Assert.That(ruleTypedTlsEvaluationResults[0].TlsEvaluatedResult.Result, Is.EqualTo(EvaluatorResult.INFORMATIONAL));
            StringAssert.StartsWith("This server does not support TLS 1.1", ruleTypedTlsEvaluationResults[0].TlsEvaluatedResult.Description);
        }

        [Test]
        public async Task NoErrorReturnsPass()
        {
            Tls11Available tls11Available = new Tls11Available();

            BouncyCastleTlsTestResult tls11ConnectionResult = new BouncyCastleTlsTestResult(null, null, null, null, null, null, new List<string>());

            TlsTestResults connectionTestResults = TlsTestDataUtil.CreateMxHostTlsResults(new Dictionary<TlsTestType, BouncyCastleTlsTestResult>
            {
                {TlsTestType.Tls11AvailableWithBestCipherSuiteSelected, tls11ConnectionResult},
            });

            List<RuleTypedTlsEvaluationResult> ruleTypedTlsEvaluationResults =
                await tls11Available.Evaluate(connectionTestResults);

            Assert.That(ruleTypedTlsEvaluationResults.Count, Is.EqualTo(1));
            Assert.That(ruleTypedTlsEvaluationResults[0].TlsEvaluatedResult.Result, Is.EqualTo(EvaluatorResult.PASS));
            Assert.That(ruleTypedTlsEvaluationResults[0].TlsEvaluatedResult.Description, Is.Null);
        }
    }
}