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
    public class TlsSecureDiffieHellmanGroupSelectedTest
    {
        private TlsSecureDiffieHellmanGroupSelected _sut;

        [SetUp]
        public void SetUp()
        {
            _sut = new TlsSecureDiffieHellmanGroupSelected();
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
                TlsTestDataUtil.CreateMxHostTlsResults(TlsTestType.TlsSecureDiffieHellmanGroupSelected, tlsConnectionResult);

            List<RuleTypedTlsEvaluationResult> evaluatorResults = await _sut.Evaluate(connectionTestResults);

            Assert.That(evaluatorResults.Count, Is.EqualTo(1));

            Assert.AreEqual(evaluatorResults[0].TlsEvaluatedResult.Result, EvaluatorResult.INCONCLUSIVE);
            StringAssert.Contains($"Error description \"{description}\".", evaluatorResults[0].TlsEvaluatedResult.Description);
        }

        [Test]
        [TestCase(TlsError.TCP_CONNECTION_FAILED)]
        [TestCase(TlsError.SESSION_INITIALIZATION_FAILED)]
        public async Task TcpErrorsShouldResultInInconclusive(TlsError tlsError)
        {
            BouncyCastleTlsTestResult tlsConnectionResult = new BouncyCastleTlsTestResult(tlsError, null, null);
           TlsTestResults connectionTestResults =
                TlsTestDataUtil.CreateMxHostTlsResults(TlsTestType.TlsSecureDiffieHellmanGroupSelected, tlsConnectionResult);

            List<RuleTypedTlsEvaluationResult> evaluatorResults = await _sut.Evaluate(connectionTestResults);

            Assert.That(evaluatorResults.Count, Is.EqualTo(1));

            Assert.AreEqual(evaluatorResults[0].TlsEvaluatedResult.Result, EvaluatorResult.INCONCLUSIVE);
        }

        [Test]
        [TestCase(TlsError.HANDSHAKE_FAILURE)]
        [TestCase(TlsError.PROTOCOL_VERSION)]
        [TestCase(TlsError.INSUFFICIENT_SECURITY)]
        public async Task ConnectionRefusedErrorsShouldResultInPass(TlsError tlsError)
        {
            BouncyCastleTlsTestResult tlsConnectionResult = new BouncyCastleTlsTestResult(tlsError, null, null);
           TlsTestResults connectionTestResults =
                TlsTestDataUtil.CreateMxHostTlsResults(TlsTestType.TlsSecureDiffieHellmanGroupSelected, tlsConnectionResult);

            List<RuleTypedTlsEvaluationResult> evaluatorResults = await _sut.Evaluate(connectionTestResults);

            Assert.That(evaluatorResults.Count, Is.EqualTo(1));

            Assert.AreEqual(evaluatorResults[0].TlsEvaluatedResult.Result, EvaluatorResult.PASS);
        }

        [Test]
        public async Task OtherErrorsShouldResultInInconclusive()
        {
            BouncyCastleTlsTestResult tlsConnectionResult = new BouncyCastleTlsTestResult(TlsError.INTERNAL_ERROR, null, null);
           TlsTestResults connectionTestResults =
                TlsTestDataUtil.CreateMxHostTlsResults(TlsTestType.TlsSecureDiffieHellmanGroupSelected, tlsConnectionResult);

            List<RuleTypedTlsEvaluationResult> evaluatorResults = await _sut.Evaluate(connectionTestResults);

            Assert.That(evaluatorResults.Count, Is.EqualTo(1));

            Assert.AreEqual(evaluatorResults[0].TlsEvaluatedResult.Result, EvaluatorResult.INCONCLUSIVE);
        }

        [Test]
        [TestCase(CurveGroup.Ffdhe2048)]
        [TestCase(CurveGroup.Ffdhe3072)]
        [TestCase(CurveGroup.Ffdhe4096)]
        [TestCase(CurveGroup.Ffdhe6144)]
        [TestCase(CurveGroup.Ffdhe8192)]
        [TestCase(CurveGroup.UnknownGroup2048)]
        [TestCase(CurveGroup.UnknownGroup3072)]
        [TestCase(CurveGroup.UnknownGroup4096)]
        [TestCase(CurveGroup.UnknownGroup6144)]
        [TestCase(CurveGroup.UnknownGroup8192)]
        public async Task GoodCurveGroupsShouldResultInAPass(CurveGroup curveGroup)
        {
            BouncyCastleTlsTestResult tlsConnectionResult = new BouncyCastleTlsTestResult(null, null, curveGroup, null, null, null, null, null);
            TlsTestResults connectionTestResults =
                TlsTestDataUtil.CreateMxHostTlsResults(TlsTestType.TlsSecureDiffieHellmanGroupSelected, tlsConnectionResult);

            List<RuleTypedTlsEvaluationResult> evaluatorResults = await _sut.Evaluate(connectionTestResults);

            Assert.That(evaluatorResults.Count, Is.EqualTo(1));

            Assert.AreEqual(evaluatorResults[0].TlsEvaluatedResult.Result, EvaluatorResult.PASS);
        }

        [Test]
        public async Task Unknown1024GroupShouldResultInAWarn()
        {
            BouncyCastleTlsTestResult tlsConnectionResult = new BouncyCastleTlsTestResult(null, null, CurveGroup.UnknownGroup1024, null, null, null, null, null);
            TlsTestResults connectionTestResults =
                TlsTestDataUtil.CreateMxHostTlsResults(TlsTestType.TlsSecureDiffieHellmanGroupSelected, tlsConnectionResult);

            List<RuleTypedTlsEvaluationResult> evaluatorResults = await _sut.Evaluate(connectionTestResults);

            Assert.That(evaluatorResults.Count, Is.EqualTo(1));

            Assert.AreEqual(evaluatorResults[0].TlsEvaluatedResult.Result, EvaluatorResult.WARNING);
        }

        [Test]
        [TestCase(CurveGroup.Java1024)]
        [TestCase(CurveGroup.Rfc2409_1024)]
        [TestCase(CurveGroup.Rfc5114_1024)]
        [TestCase(CurveGroup.Unknown)]
        public async Task Known1024GroupShouldResultInAFail(CurveGroup curveGroup)
        {
            BouncyCastleTlsTestResult tlsConnectionResult = new BouncyCastleTlsTestResult(null, null, curveGroup, null, null, null, null, null);
            TlsTestResults connectionTestResults =
                TlsTestDataUtil.CreateMxHostTlsResults(TlsTestType.TlsSecureDiffieHellmanGroupSelected, tlsConnectionResult);

            List<RuleTypedTlsEvaluationResult> evaluatorResults = await _sut.Evaluate(connectionTestResults);

            Assert.That(evaluatorResults.Count, Is.EqualTo(1));

            Assert.AreEqual(evaluatorResults[0].TlsEvaluatedResult.Result, EvaluatorResult.FAIL);
        }

        [Test]
        public async Task NoCurveGroupShouldResultInInconslusive()
        {
            BouncyCastleTlsTestResult tlsConnectionResult = new BouncyCastleTlsTestResult(null, null, null, null, null, null, null, null);
            TlsTestResults connectionTestResults =
                TlsTestDataUtil.CreateMxHostTlsResults(TlsTestType.TlsSecureDiffieHellmanGroupSelected, tlsConnectionResult);

            List<RuleTypedTlsEvaluationResult> evaluatorResults = await _sut.Evaluate(connectionTestResults);

            Assert.That(evaluatorResults.Count, Is.EqualTo(1));

            Assert.AreEqual(evaluatorResults[0].TlsEvaluatedResult.Result, EvaluatorResult.INCONCLUSIVE);
        }
    }
}
