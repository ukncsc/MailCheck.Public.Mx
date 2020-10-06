using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FakeItEasy;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.TlsEvaluator;
using MailCheck.Mx.TlsEvaluator.Rules.TlsEvaluation;
using MailCheck.Mx.TlsEvaluator.Util;
using NUnit.Framework;

namespace MailCheck.Mx.TlsEvaluator.Test.Rules.TlsEvaluation
{
    [TestFixture]
    public class EvaluatorTests
    {
        private IRule<TlsResultsEvaluated, RuleTypedTlsEvaluationResult> _rule1;
        private IRule<TlsResultsEvaluated, RuleTypedTlsEvaluationResult> _rule2;

        private Evaluator<TlsResultsEvaluated, RuleTypedTlsEvaluationResult> _evaluator;

        [SetUp]
        public void SetUp()
        {
            _rule1 = A.Fake<IRule<TlsResultsEvaluated, RuleTypedTlsEvaluationResult>>();
            _rule2 = A.Fake<IRule<TlsResultsEvaluated, RuleTypedTlsEvaluationResult>>();

            _evaluator = new Evaluator<TlsResultsEvaluated, RuleTypedTlsEvaluationResult>(new[] { _rule1, _rule2 });
        }

        [Test]
        public async Task AllRulePassNoErrors()
        {
            TlsResultsEvaluated EvaluatorResults = A.Fake<TlsResultsEvaluated>();

            A.CallTo(() => _rule1.Evaluate(EvaluatorResults)).Returns(new List<RuleTypedTlsEvaluationResult>());
            A.CallTo(() => _rule2.Evaluate(EvaluatorResults)).Returns(new List<RuleTypedTlsEvaluationResult>());

           var evaluationResult = await _evaluator.Evaluate(EvaluatorResults);

            Assert.That(evaluationResult.Messages, Is.Empty);
        }

        [Test]
        public async Task AllRulesFailErrors()
        {
            TlsResultsEvaluated evaluatorResults = A.Fake<TlsResultsEvaluated>();

            var evaluationError1 = new RuleTypedTlsEvaluationResult(TlsTestType.Tls10Available, Guid.NewGuid(), EvaluatorResult.FAIL, "Rule 1 Failed");
            var evaluationError2 = new RuleTypedTlsEvaluationResult(TlsTestType.Tls10Available, Guid.NewGuid(), EvaluatorResult.FAIL, "Rule 2 Failed");

            A.CallTo(() => _rule1.Evaluate(evaluatorResults)).Returns(new List<RuleTypedTlsEvaluationResult> { evaluationError1 });
            A.CallTo(() => _rule2.Evaluate(evaluatorResults)).Returns(new List<RuleTypedTlsEvaluationResult> { evaluationError2 });

            EvaluationResult<TlsResultsEvaluated, RuleTypedTlsEvaluationResult> evaluationResult = await _evaluator.Evaluate(evaluatorResults);

            Assert.That(evaluationResult.Messages.Count, Is.EqualTo(2));
            Assert.That(evaluationResult.Messages[0], Is.EqualTo(evaluationError1));
            Assert.That(evaluationResult.Messages[1], Is.EqualTo(evaluationError2));
        }

        [Test]
        public async Task MixedRuleFailuresErrorsAsExpected()
        {
            TlsResultsEvaluated evaluatorResults = A.Fake<TlsResultsEvaluated>();

            var evaluationError1 = new RuleTypedTlsEvaluationResult(TlsTestType.Tls10Available, Guid.NewGuid(), EvaluatorResult.FAIL, "Rule 1 Failed");

            A.CallTo(() => _rule1.Evaluate(evaluatorResults)).Returns(new List<RuleTypedTlsEvaluationResult> { evaluationError1 });
            A.CallTo(() => _rule2.Evaluate(evaluatorResults)).Returns(new List<RuleTypedTlsEvaluationResult>());

            EvaluationResult<TlsResultsEvaluated, RuleTypedTlsEvaluationResult> evaluationResult = await _evaluator.Evaluate(evaluatorResults);

            Assert.That(evaluationResult.Messages.Count, Is.EqualTo(1));
            Assert.That(evaluationResult.Messages[0], Is.EqualTo(evaluationError1));
        }

        [Test]
        public async Task StopRuleFailureHaltProcessing()
        {
            TlsResultsEvaluated evaluatorResults = A.Fake<TlsResultsEvaluated>();

            var evaluationError1 = new RuleTypedTlsEvaluationResult(TlsTestType.Tls10Available, Guid.NewGuid(), EvaluatorResult.FAIL, "Rule 1 Failed");
            var evaluationError2 = new RuleTypedTlsEvaluationResult(TlsTestType.Tls10Available, Guid.NewGuid(), EvaluatorResult.WARNING, "Rule 2 Failed");

            A.CallTo(() => _rule1.Evaluate(evaluatorResults)).Returns(new List<RuleTypedTlsEvaluationResult> { evaluationError1 });
            A.CallTo(() => _rule1.IsStopRule).Returns(true);

            A.CallTo(() => _rule2.Evaluate(evaluatorResults)).Returns(new List<RuleTypedTlsEvaluationResult> { evaluationError2 });

            EvaluationResult<TlsResultsEvaluated, RuleTypedTlsEvaluationResult> evaluationResult = await _evaluator.Evaluate(evaluatorResults);

            Assert.That(evaluationResult.Messages.Count, Is.EqualTo(1));
            Assert.That(evaluationResult.Messages[0], Is.EqualTo(evaluationError1));
        }

        [Test]
        public async Task RulesInDifferentCategoriesStillProcessedOnOtherCategoryFailure()
        {
            TlsResultsEvaluated evaluatorResults = A.Fake<TlsResultsEvaluated>();

            var evaluationError1 = new RuleTypedTlsEvaluationResult(TlsTestType.Tls10Available, Guid.NewGuid(), EvaluatorResult.FAIL, "Rule 1 Failed");
            var evaluationError2 = new RuleTypedTlsEvaluationResult(TlsTestType.Tls10Available, Guid.NewGuid(), EvaluatorResult.WARNING, "Rule 2 Failed");

            A.CallTo(() => _rule1.Evaluate(evaluatorResults)).Returns(new List<RuleTypedTlsEvaluationResult> { evaluationError1 });
            A.CallTo(() => _rule1.Category).Returns("Cat1");
            A.CallTo(() => _rule1.IsStopRule).Returns(true);

            A.CallTo(() => _rule2.Evaluate(evaluatorResults)).Returns(new List<RuleTypedTlsEvaluationResult> { evaluationError2 });
            A.CallTo(() => _rule2.Category).Returns("Cat2");

            EvaluationResult<TlsResultsEvaluated, RuleTypedTlsEvaluationResult> evaluationResult = await _evaluator.Evaluate(evaluatorResults);

            Assert.That(evaluationResult.Messages.Count, Is.EqualTo(2));
            Assert.That(evaluationResult.Messages[0], Is.EqualTo(evaluationError1));
            Assert.That(evaluationResult.Messages[1], Is.EqualTo(evaluationError2));
        }

        [Test]
        public async Task FailureEvaluatorUsedWhenProvided()
        {
            TlsResultsEvaluated evaluatorResults = A.Fake<TlsResultsEvaluated>();

            var evaluationError1 = new RuleTypedTlsEvaluationResult(TlsTestType.Tls10Available, Guid.NewGuid(), EvaluatorResult.PASS, "Rule 1 Failed");
            var evaluationError2 = new RuleTypedTlsEvaluationResult(TlsTestType.Tls10Available, Guid.NewGuid(), EvaluatorResult.WARNING, "Rule 2 Failed");

            A.CallTo(() => _rule1.Evaluate(evaluatorResults)).Returns(new List<RuleTypedTlsEvaluationResult> { evaluationError1 });
            A.CallTo(() => _rule1.Category).Returns("Cat1");
            A.CallTo(() => _rule1.IsStopRule).Returns(true);

            A.CallTo(() => _rule2.Evaluate(evaluatorResults)).Returns(new List<RuleTypedTlsEvaluationResult> { evaluationError2 });
            A.CallTo(() => _rule2.Category).Returns("Cat1");

            EvaluationResult<TlsResultsEvaluated, RuleTypedTlsEvaluationResult> evaluationResult = await _evaluator.Evaluate(evaluatorResults, _ => true);

            Assert.That(evaluationResult.Messages.Count, Is.EqualTo(1));
        }
    }
}
