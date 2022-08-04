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

        [TestCaseSource(nameof(ExerciseDomainStatusEvaluatorPermutations))]
        public void ShouldDetermineCorrectStatus(DomainStatusEvaluatorTestCase testCase)
        {
            List<EvaluatorResult?> configErrors = testCase.ConfigErrors;
            List<Error> certErrors = testCase.CertErrors?.Select(x => new Error(x, "", "")).ToList();

            var result = _domainStatusEvaluator.GetStatus(configErrors, certErrors);

            Assert.AreEqual(testCase.ExpectedStatus, result);
        }

        private static IEnumerable<DomainStatusEvaluatorTestCase> ExerciseDomainStatusEvaluatorPermutations()
        {
            yield return new DomainStatusEvaluatorTestCase
            {
                ExpectedStatus = Status.Success,
                CertErrors = null,
                ConfigErrors = null,
                Description = "Null cert or config errors evaluates to success"
            };
            yield return new DomainStatusEvaluatorTestCase
            {
                ExpectedStatus = Status.Success,
                CertErrors = new List<ErrorType>(),
                ConfigErrors = new List<EvaluatorResult?>(),
                Description = "Empty cert or config errors evaluates to success"
            };
            yield return new DomainStatusEvaluatorTestCase
            {
                ExpectedStatus = Status.Info,
                CertErrors = new List<ErrorType> { ErrorType.Inconclusive },
                ConfigErrors = null,
                Description = "Inconclusive cert, null config errors evaluates to info"
            };
            yield return new DomainStatusEvaluatorTestCase 
            { 
                ExpectedStatus = Status.Warning, 
                CertErrors = new List<ErrorType> { ErrorType.Warning }, 
                ConfigErrors = null,
                Description = "Warning cert, null config errors evaluates to warning"
            };
            yield return new DomainStatusEvaluatorTestCase 
            { 
                ExpectedStatus = Status.Error, 
                CertErrors = new List<ErrorType> { ErrorType.Error }, 
                ConfigErrors = null,
                Description = "Error cert, null config errors evaluates to error"
            };
            yield return new DomainStatusEvaluatorTestCase 
            { 
                ExpectedStatus = Status.Error, 
                CertErrors = new List<ErrorType> { ErrorType.Error, ErrorType.Inconclusive }, 
                ConfigErrors = null,
                Description = "Error and inconc cert, null config errors evaluates to error"
            };
            yield return new DomainStatusEvaluatorTestCase 
            { 
                ExpectedStatus = Status.Error, 
                CertErrors = new List<ErrorType> { ErrorType.Error, ErrorType.Warning }, 
                ConfigErrors = null,
                Description = "Error and inconc cert, null config errors evaluates to error"
            };

            yield return new DomainStatusEvaluatorTestCase 
            { 
                ExpectedStatus = Status.Info, 
                CertErrors = null, 
                ConfigErrors = new List<EvaluatorResult?> { EvaluatorResult.INCONCLUSIVE },
                Description = "null cert, incon config errors evaluates to info"
            };
            yield return new DomainStatusEvaluatorTestCase 
            { 
                ExpectedStatus = Status.Info, 
                CertErrors = null, 
                ConfigErrors = new List<EvaluatorResult?> { EvaluatorResult.INFORMATIONAL },
                Description = "null cert, info config errors evaluates to info"
            };
            yield return new DomainStatusEvaluatorTestCase 
            { 
                ExpectedStatus = Status.Info, 
                CertErrors = null, 
                ConfigErrors = new List<EvaluatorResult?> { EvaluatorResult.UNKNOWN },
                Description = "null cert, unknown config errors evaluates to info"
            };
            yield return new DomainStatusEvaluatorTestCase 
            { 
                ExpectedStatus = Status.Warning, 
                CertErrors = null, 
                ConfigErrors = new List<EvaluatorResult?> { EvaluatorResult.WARNING },
                Description = "null cert, warning config errors evaluates to warning"
            };
            yield return new DomainStatusEvaluatorTestCase 
            { 
                ExpectedStatus = Status.Warning, 
                CertErrors = null, 
                ConfigErrors = new List<EvaluatorResult?> { EvaluatorResult.INCONCLUSIVE, EvaluatorResult.WARNING },
                Description = "null cert + inco, warning config errors evaluates to warning"
            };

            yield return new DomainStatusEvaluatorTestCase 
            {
                ExpectedStatus = Status.Error, 
                CertErrors = null, 
                ConfigErrors = new List<EvaluatorResult?> { EvaluatorResult.FAIL },
                Description = "null cert, fail config errors evaluates to error"
            };
            yield return new DomainStatusEvaluatorTestCase 
            { 
                ExpectedStatus = Status.Error, 
                CertErrors = null, 
                ConfigErrors = new List<EvaluatorResult?> { EvaluatorResult.INCONCLUSIVE, EvaluatorResult.FAIL },
                Description = "null cert + inco and fail config errors evaluates to error"
            };
            yield return new DomainStatusEvaluatorTestCase 
            { 
                ExpectedStatus = Status.Error, 
                CertErrors = null, 
                ConfigErrors = new List<EvaluatorResult?> { EvaluatorResult.WARNING, EvaluatorResult.FAIL },
                Description = "null cert + warning and fail config errors evaluates to error"
            };

            yield return new DomainStatusEvaluatorTestCase 
            { 
                ExpectedStatus = Status.Info, 
                CertErrors = new List<ErrorType> { ErrorType.Inconclusive }, 
                ConfigErrors = new List<EvaluatorResult?> { EvaluatorResult.INCONCLUSIVE, EvaluatorResult.INFORMATIONAL, EvaluatorResult.UNKNOWN },
                Description = "incon cert + incon, info and unknown config errors evaluates to info"
            };
            yield return new DomainStatusEvaluatorTestCase 
            { 
                ExpectedStatus = Status.Warning, 
                CertErrors = new List<ErrorType> { ErrorType.Warning }, 
                ConfigErrors = new List<EvaluatorResult?> { EvaluatorResult.WARNING },
                Description = "warning cert + warning config errors evaluates to warning"
            };
            yield return new DomainStatusEvaluatorTestCase 
            { 
                ExpectedStatus = Status.Error, 
                CertErrors = new List<ErrorType> { ErrorType.Error }, 
                ConfigErrors = new List<EvaluatorResult?> { EvaluatorResult.FAIL },
                Description = "error cert + fail config errors evaluates to error"
            };

            yield return new DomainStatusEvaluatorTestCase 
            { 
                ExpectedStatus = Status.Warning, 
                CertErrors = new List<ErrorType> { ErrorType.Warning }, 
                ConfigErrors = new List<EvaluatorResult?> { EvaluatorResult.INCONCLUSIVE },
                Description = "warning cert + incon config errors evaluates to warning"
            };
            yield return new DomainStatusEvaluatorTestCase 
            { 
                ExpectedStatus = Status.Warning, 
                CertErrors = new List<ErrorType> { ErrorType.Inconclusive }, 
                ConfigErrors = new List<EvaluatorResult?> { EvaluatorResult.WARNING },
                Description = "incon cert + warning config errors evaluates to warning"
            };
            yield return new DomainStatusEvaluatorTestCase 
            { 
                ExpectedStatus = Status.Error, 
                CertErrors = new List<ErrorType> { ErrorType.Error }, 
                ConfigErrors = new List<EvaluatorResult?> { EvaluatorResult.WARNING },
                Description = "error cert + warning config errors evaluates to error"
            };
            yield return new DomainStatusEvaluatorTestCase 
            {
                ExpectedStatus = Status.Error, 
                CertErrors = new List<ErrorType> { ErrorType.Warning }, 
                ConfigErrors = new List<EvaluatorResult?> { EvaluatorResult.FAIL },
                Description = "warning cert + fail config errors evaluates to error"
            };
            yield return new DomainStatusEvaluatorTestCase 
            { 
                ExpectedStatus = Status.Error, 
                CertErrors = new List<ErrorType> { ErrorType.Error }, 
                ConfigErrors = new List<EvaluatorResult?> { EvaluatorResult.INCONCLUSIVE },
                Description = "error cert + incon config errors evaluates to error"
            };
            yield return new DomainStatusEvaluatorTestCase 
            { 
                ExpectedStatus = Status.Error, 
                CertErrors = new List<ErrorType> { ErrorType.Inconclusive }, 
                ConfigErrors = new List<EvaluatorResult?> { EvaluatorResult.FAIL },
                Description = "incon cert + fail config errors evaluates to error"
            };
            yield return new DomainStatusEvaluatorTestCase 
            {
                ExpectedStatus = Status.Warning, 
                CertErrors = new List<ErrorType> { ErrorType.Warning }, 
                ConfigErrors = new List<EvaluatorResult?> { null, null, null },
                Description = "warning cert + null tlsconfig results should evaluate to warning"
            };
            yield return new DomainStatusEvaluatorTestCase 
            {
                ExpectedStatus = Status.Error, 
                CertErrors = new List<ErrorType> { ErrorType.Warning }, 
                ConfigErrors = new List<EvaluatorResult?> { EvaluatorResult.FAIL, null, null, null },
                Description = "null results should be ignored: warning cert + fail config errors evaluates to error"
            };
            yield return new DomainStatusEvaluatorTestCase 
            { 
                ExpectedStatus = Status.Error, 
                CertErrors = new List<ErrorType> { ErrorType.Error }, 
                ConfigErrors = new List<EvaluatorResult?> { EvaluatorResult.INCONCLUSIVE, null, null, null },
                Description = "null results should be ignored: error cert + incon config errors evaluates to error"
            };
            yield return new DomainStatusEvaluatorTestCase 
            { 
                ExpectedStatus = Status.Error, 
                CertErrors = new List<ErrorType> { ErrorType.Inconclusive }, 
                ConfigErrors = new List<EvaluatorResult?> { EvaluatorResult.FAIL, null, null, null },
                Description = "null results should be ignored: incon cert + fail config errors evaluates to error"
            };
        }
    }

    public class DomainStatusEvaluatorTestCase
    {
        public List<EvaluatorResult?> ConfigErrors { get; set; }
        public List<ErrorType> CertErrors { get; set; }
        public Status ExpectedStatus { get; set; }
        public string Description { get; set; }
    }
}
