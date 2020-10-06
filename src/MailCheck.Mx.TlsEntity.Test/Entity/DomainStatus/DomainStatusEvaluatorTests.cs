using System;
using System.Collections.Generic;
using System.Linq;
using MailCheck.DomainStatus.Contracts;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.TlsEntity.Entity.DomainStatus;
using NUnit.Framework;

namespace MailCheck.Mx.TlsEntity.Test.Entity.DomainStatus
{
    [TestFixture]
    public class DomainStatusEvaluatorTests
    {
        private DomainStatusEvaluator _domainStatusEvaluator;

        [SetUp]
        public void SetUp()
        {
            _domainStatusEvaluator = new DomainStatusEvaluator();
        }

        [TestCase(Status.Success, null, null)]
        [TestCase(Status.Success, new ErrorType[] { }, new EvaluatorResult[] { })]

        [TestCase(Status.Info, new[] { ErrorType.Inconclusive }, null)]
        [TestCase(Status.Warning, new[] { ErrorType.Warning }, null)]
        [TestCase(Status.Warning, new[] { ErrorType.Inconclusive, ErrorType.Warning }, null)]
        [TestCase(Status.Error, new[] { ErrorType.Error }, null)]
        [TestCase(Status.Error, new[] { ErrorType.Inconclusive, ErrorType.Error }, null)]
        [TestCase(Status.Error, new[] { ErrorType.Warning, ErrorType.Error }, null)]

        [TestCase(Status.Info, null, new[] { EvaluatorResult.INCONCLUSIVE })]
        [TestCase(Status.Info, null, new[] { EvaluatorResult.INFORMATIONAL })]
        [TestCase(Status.Info, null, new[] { EvaluatorResult.UNKNOWN })]
        [TestCase(Status.Warning, null, new[] { EvaluatorResult.WARNING })]
        [TestCase(Status.Warning, null, new[] { EvaluatorResult.INCONCLUSIVE, EvaluatorResult.WARNING })]
        [TestCase(Status.Error, null, new[] { EvaluatorResult.FAIL })]
        [TestCase(Status.Error, null, new[] { EvaluatorResult.INCONCLUSIVE, EvaluatorResult.FAIL })]
        [TestCase(Status.Error, null, new[] { EvaluatorResult.WARNING, EvaluatorResult.FAIL })]

        [TestCase(Status.Info, new[] { ErrorType.Inconclusive }, new[] { EvaluatorResult.INCONCLUSIVE, EvaluatorResult.INFORMATIONAL, EvaluatorResult.UNKNOWN })]
        [TestCase(Status.Warning, new[] { ErrorType.Warning }, new[] { EvaluatorResult.WARNING })]
        [TestCase(Status.Error, new[] { ErrorType.Error }, new[] { EvaluatorResult.FAIL })]

        [TestCase(Status.Warning, new[] { ErrorType.Warning }, new[] { EvaluatorResult.INCONCLUSIVE })]
        [TestCase(Status.Warning, new[] { ErrorType.Inconclusive }, new[] { EvaluatorResult.WARNING })]
        [TestCase(Status.Error, new[] { ErrorType.Error }, new[] { EvaluatorResult.WARNING })]
        [TestCase(Status.Error, new[] { ErrorType.Warning }, new[] { EvaluatorResult.FAIL })]
        [TestCase(Status.Error, new[] { ErrorType.Error }, new[] { EvaluatorResult.INCONCLUSIVE })]
        [TestCase(Status.Error, new[] { ErrorType.Inconclusive }, new[] { EvaluatorResult.FAIL })]

        public void ShouldDetermineCorrectStatus(Status expectedStatus, ErrorType[] certificateErrors, EvaluatorResult[] evaluatorResults)
        {
            List<TlsEvaluatedResult> tlsEvaluatedResults = evaluatorResults?.Select(x => new TlsEvaluatedResult(Guid.Empty, x)).ToList();
            List<Error> errors = certificateErrors?.Select(x => new Error(x, string.Empty)).ToList();

            var result = _domainStatusEvaluator.GetStatus(tlsEvaluatedResults, errors);

            Assert.AreEqual(expectedStatus, result);
        }
    }
}
