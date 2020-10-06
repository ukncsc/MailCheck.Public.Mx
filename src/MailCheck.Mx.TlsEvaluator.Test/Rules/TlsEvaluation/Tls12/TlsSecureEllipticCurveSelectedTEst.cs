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
    public class TlsSecureEllipticCurveSelectedTest
    {
        private TlsSecureEllipticCurveSelected _sut;

        [SetUp]
        public void SetUp()
        {
            _sut = new TlsSecureEllipticCurveSelected();
        }

        [Test]
        [TestCase(TlsError.TCP_CONNECTION_FAILED)]
        [TestCase(TlsError.SESSION_INITIALIZATION_FAILED)]
        public async Task TcpErrorsShouldResultInInconclusive(TlsError tlsError)
        {
            BouncyCastleTlsTestResult tlsConnectionResult = new BouncyCastleTlsTestResult(tlsError, null, null);
            TlsTestResults connectionTestResults =
                TlsTestDataUtil.CreateMxHostTlsResults(TlsTestType.TlsSecureEllipticCurveSelected, tlsConnectionResult);

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
                TlsTestDataUtil.CreateMxHostTlsResults(TlsTestType.TlsSecureEllipticCurveSelected, tlsConnectionResult);

            List<RuleTypedTlsEvaluationResult> evaluatorResults = await _sut.Evaluate(connectionTestResults);

            Assert.That(evaluatorResults.Count, Is.EqualTo(1));

            Assert.AreEqual(evaluatorResults[0].TlsEvaluatedResult.Result, EvaluatorResult.INCONCLUSIVE);
            StringAssert.Contains($"Error description \"{description}\".", evaluatorResults[0].TlsEvaluatedResult.Description);
        }

        [Test]
        public async Task AnErrorShouldResultInInconslusive()
        {
            TlsTestResults connectionTestResults = TlsTestDataUtil.CreateMxHostTlsResults(TlsTestType.TlsSecureEllipticCurveSelected,
                new BouncyCastleTlsTestResult(TlsError.CERTIFICATE_UNOBTAINABLE, null, null));

            List<RuleTypedTlsEvaluationResult> evaluatorResults = await _sut.Evaluate(connectionTestResults);

            Assert.That(evaluatorResults.Count, Is.EqualTo(1));

            Assert.AreEqual(evaluatorResults[0].TlsEvaluatedResult.Result, EvaluatorResult.INCONCLUSIVE);
        }

        [Test]
        public async Task UnaccountedForCurveShouldResultInInconclusive()
        {
            BouncyCastleTlsTestResult tlsConnectionResult = new BouncyCastleTlsTestResult(null, null, null, null, null, null, null, null);
            TlsTestResults connectionTestResults =
                TlsTestDataUtil.CreateMxHostTlsResults(TlsTestType.TlsSecureEllipticCurveSelected, tlsConnectionResult);

            List<RuleTypedTlsEvaluationResult> evaluatorResults = await _sut.Evaluate(connectionTestResults);

            Assert.That(evaluatorResults.Count, Is.EqualTo(1));

            Assert.AreEqual(evaluatorResults[0].TlsEvaluatedResult.Result, EvaluatorResult.INCONCLUSIVE);
        }

        [Test]
        [TestCase(CurveGroup.Unknown)]
        [TestCase(CurveGroup.Secp160k1)]
        [TestCase(CurveGroup.Secp160r1)]
        [TestCase(CurveGroup.Secp160r2)]
        [TestCase(CurveGroup.Secp192k1)]
        [TestCase(CurveGroup.Secp192r1)]
        [TestCase(CurveGroup.Secp224k1)]
        [TestCase(CurveGroup.Secp224r1)]
        [TestCase(CurveGroup.Sect163k1)]
        [TestCase(CurveGroup.Sect163r1)]
        [TestCase(CurveGroup.Sect163r2)]
        [TestCase(CurveGroup.Sect193r1)]
        [TestCase(CurveGroup.Sect193r2)]
        [TestCase(CurveGroup.Sect233k1)]
        [TestCase(CurveGroup.Sect233r1)]
        [TestCase(CurveGroup.Sect239k1)]
        public async Task CurvesWithCurveNumberLessThan256ShouldResultInAFail(CurveGroup curveGroup)
        {
            BouncyCastleTlsTestResult tlsConnectionResult = new BouncyCastleTlsTestResult(null, null, curveGroup, null, null, null, null, null);
            TlsTestResults connectionTestResults =
                TlsTestDataUtil.CreateMxHostTlsResults(TlsTestType.TlsSecureEllipticCurveSelected, tlsConnectionResult);

            List<RuleTypedTlsEvaluationResult> evaluatorResults = await _sut.Evaluate(connectionTestResults);

            Assert.That(evaluatorResults.Count, Is.EqualTo(1));

            Assert.AreEqual(evaluatorResults[0].TlsEvaluatedResult.Result, EvaluatorResult.FAIL);
        }

        [Test]
        [TestCase(CurveGroup.Secp256k1)]
        [TestCase(CurveGroup.Secp256r1)]
        [TestCase(CurveGroup.Secp384r1)]
        [TestCase(CurveGroup.Secp521r1)]
        [TestCase(CurveGroup.Sect283k1)]
        [TestCase(CurveGroup.Sect283r1)]
        [TestCase(CurveGroup.Sect409k1)]
        [TestCase(CurveGroup.Sect409r1)]
        [TestCase(CurveGroup.Sect571k1)]
        [TestCase(CurveGroup.Sect571r1)]
        public async Task CurvesWithCurveNumberGreaterThan256ShouldResultInAPass(CurveGroup curveGroup)
        {
            BouncyCastleTlsTestResult tlsConnectionResult = new BouncyCastleTlsTestResult(null, null, curveGroup, null, null, null, null, null);
            TlsTestResults connectionTestResults =
                TlsTestDataUtil.CreateMxHostTlsResults(TlsTestType.TlsSecureEllipticCurveSelected, tlsConnectionResult);

            List<RuleTypedTlsEvaluationResult> evaluatorResults = await _sut.Evaluate(connectionTestResults);

            Assert.That(evaluatorResults.Count, Is.EqualTo(1));

            Assert.AreEqual(evaluatorResults[0].TlsEvaluatedResult.Result, EvaluatorResult.PASS);
        }
    }
}
