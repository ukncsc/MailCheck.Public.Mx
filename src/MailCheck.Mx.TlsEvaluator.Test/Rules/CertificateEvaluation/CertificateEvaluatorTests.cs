using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FakeItEasy;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Domain;
using NUnit.Framework;

namespace MailCheck.Mx.TlsEvaluator.Test.Rules.CertificateEvaluation
{
    [TestFixture]
    public class CertificateEvaluatorTests
    {
        private IRule<HostCertificates> _rule1;
        private IRule<HostCertificates> _rule2;
        private CertificateEvaluator _evaluator;
        private IPreprocessorComposite<HostCertificates> _preprocessor;

        [SetUp]
        public void SetUp()
        {
            _rule1 = A.Fake<IRule<HostCertificates>>();
            _rule2 = A.Fake<IRule<HostCertificates>>();
            _preprocessor = A.Fake<IPreprocessorComposite<HostCertificates>>();

            _evaluator = new CertificateEvaluator(new [] {_rule1, _rule2}, _preprocessor);
        }

        [Test]
        public async Task AllRulePassNoErrors()
        {
            HostCertificates hostCertificates = A.Fake<HostCertificates>();

            A.CallTo(() => _rule1.Evaluate(hostCertificates)).Returns(new List<EvaluationError>());
            A.CallTo(() => _rule2.Evaluate(hostCertificates)).Returns(new List<EvaluationError>());

            EvaluationResult<HostCertificates> evaluationResult = await _evaluator.Evaluate(hostCertificates);

            Assert.That(evaluationResult.Errors, Is.Empty);
        }

        [Test]
        public async Task AllRulesFailErrors()
        {
            HostCertificates hostCertificates = A.Fake<HostCertificates>();

            var evaluationError1 = new EvaluationError(new Guid(), "mailcheck.tlsCert.testName1", EvaluationErrorType.Error, "Rule 1 Failed");
            var evaluationError2 = new EvaluationError(new Guid(), "mailcheck.tlsCert.testName2", EvaluationErrorType.Warning, "Rule 2 Failed");

            A.CallTo(() => _rule1.Evaluate(hostCertificates)).Returns(new List<EvaluationError> { evaluationError1 } );
            A.CallTo(() => _rule2.Evaluate(hostCertificates)).Returns(new List<EvaluationError> { evaluationError2 } );

            EvaluationResult<HostCertificates> evaluationResult = await _evaluator.Evaluate(hostCertificates);

            Assert.That(evaluationResult.Errors.Count, Is.EqualTo(2));
            Assert.That(evaluationResult.Errors[0], Is.EqualTo(evaluationError1));
            Assert.That(evaluationResult.Errors[1], Is.EqualTo(evaluationError2));
        }

        [Test]
        public async Task MixedRuleFailuresErrorsAsExpected()
        {
            HostCertificates hostCertificates = A.Fake<HostCertificates>();

            var evaluationError1 = new EvaluationError(new Guid(), "mailcheck.tlsCert.testName1", EvaluationErrorType.Error, "Rule 1 Failed");

            A.CallTo(() => _rule1.Evaluate(hostCertificates)).Returns(new List<EvaluationError> { evaluationError1 });
            A.CallTo(() => _rule2.Evaluate(hostCertificates)).Returns(new List<EvaluationError>());

            EvaluationResult<HostCertificates> evaluationResult = await _evaluator.Evaluate(hostCertificates);

            Assert.That(evaluationResult.Errors.Count, Is.EqualTo(1));
            Assert.That(evaluationResult.Errors[0], Is.EqualTo(evaluationError1));
        }

        [Test]
        public async Task StopRuleFailureHaltProcessing()
        {
            HostCertificates hostCertificates = A.Fake<HostCertificates>();

            var evaluationError1 = new EvaluationError(new Guid(), "mailcheck.tlsCert.testName1", EvaluationErrorType.Error, "Rule 1 Failed");
            var evaluationError2 = new EvaluationError(new Guid(), "mailcheck.tlsCert.testName2", EvaluationErrorType.Warning, "Rule 2 Failed");

            A.CallTo(() => _rule1.Evaluate(hostCertificates)).Returns(new List<EvaluationError> { evaluationError1 });
            A.CallTo(() => _rule1.IsStopRule).Returns(true);

            A.CallTo(() => _rule2.Evaluate(hostCertificates)).Returns(new List<EvaluationError> { evaluationError2 });

            EvaluationResult<HostCertificates> evaluationResult = await _evaluator.Evaluate(hostCertificates);

            Assert.That(evaluationResult.Errors.Count, Is.EqualTo(1));
            Assert.That(evaluationResult.Errors[0], Is.EqualTo(evaluationError1));
        }
    }
}
