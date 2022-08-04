using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FakeItEasy;
using MailCheck.Common.Contracts.Advisories;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Simplified;
using MailCheck.Mx.SimplifiedTlsTester.Domain;
using MailCheck.Mx.SimplifiedTlsTester.Rules;
using MailCheck.Mx.SimplifiedTlsTester.TestRunner;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using MessageType = MailCheck.Common.Contracts.Advisories.MessageType;
using TestContext = MailCheck.Mx.SimplifiedTlsTester.Domain.TestContext;

namespace MailCheck.Mx.SimplifiedTlsTester.Test.TestRunner
{
    [TestFixture]
    public class TestRunnerTests
    {
        private SimplifiedTlsTester.TestRunner.TestRunner _testRunner;
        private ITlsTester _tester;
        private ITlsRule _firstRule;
        private ITlsRule _lastRule;

        [SetUp]
        public void SetUp()
        {
            _firstRule = A.Fake<ITlsRule>();
            _lastRule = A.Fake<ITlsRule>();

            LinkedList<ITlsRule> stages = new LinkedList<ITlsRule>(new ITlsRule[] { _firstRule, _lastRule, null });

            _tester = A.Fake<ITlsTester>();
            _testRunner = new SimplifiedTlsTester.TestRunner.TestRunner(_tester, A.Fake<ILogger<SimplifiedTlsTester.TestRunner.TestRunner>>(), stages);
        }

        [Test]
        public async Task RunExecutesAllRulesInOrder()
        {
            A.CallTo(() => _firstRule.TestCriteria).Returns(new TestCriteria {Name = "firstTestCriteria"});
            A.CallTo(() => _firstRule.Evaluate(A<TestContext>._, A<BouncyCastleTlsTestResult>._))
                .ReturnsLazily((TestContext context, BouncyCastleTlsTestResult _) => context.NextTest);

            A.CallTo(() => _lastRule.TestCriteria).Returns(new TestCriteria { Name = "secondTestCriteria" });
            A.CallTo(() => _lastRule.Evaluate(A<TestContext>._, A<BouncyCastleTlsTestResult>._))
                .ReturnsLazily((TestContext context, BouncyCastleTlsTestResult _) => context.NextTest);

            BouncyCastleTlsTestResult firstTestResult = new BouncyCastleTlsTestResult(TlsVersion.TlsV13, CipherSuite.TLS_CHACHA20_POLY1305_SHA256, null, null, TlsError.ACCESS_DENIED, "FirstErrorDescription", null);
            BouncyCastleTlsTestResult lastTestResult = new BouncyCastleTlsTestResult(TlsVersion.TlsV12, CipherSuite.TLS_AES_128_CCM_8_SHA256, null, null, TlsError.TCP_CONNECTION_FAILED, "LastErrorDescription", null);
            
            A.CallTo(() => _tester.RunTest(_firstRule.TestCriteria, "testIpAddress")).Returns(firstTestResult);
            A.CallTo(() => _tester.RunTest(_lastRule.TestCriteria, "testIpAddress")).Returns(lastTestResult);
            
            SimplifiedTlsTestResults result = await _testRunner.Run("testIpAddress");

            Assert.AreEqual(2, result.SimplifiedTlsConnectionResults.Count);

            Assert.AreEqual("firstTestCriteria", result.SimplifiedTlsConnectionResults[0].TestName);
            Assert.AreEqual("TLS_CHACHA20_POLY1305_SHA256", result.SimplifiedTlsConnectionResults[0].CipherSuite);
            Assert.AreEqual("ACCESS_DENIED", result.SimplifiedTlsConnectionResults[0].Error);
            Assert.AreEqual("FirstErrorDescription", result.SimplifiedTlsConnectionResults[0].ErrorDescription);

            Assert.AreEqual("secondTestCriteria", result.SimplifiedTlsConnectionResults[1].TestName);
            Assert.AreEqual("TLS_AES_128_CCM_8_SHA256", result.SimplifiedTlsConnectionResults[1].CipherSuite);
            Assert.AreEqual("TCP_CONNECTION_FAILED", result.SimplifiedTlsConnectionResults[1].Error);
            Assert.AreEqual("LastErrorDescription", result.SimplifiedTlsConnectionResults[1].ErrorDescription);
        }

        [Test]
        public async Task RunKeepsAdvisoriesIfNotInconclusive()
        {
            NamedAdvisory firstRuleAdvisory = new NamedAdvisory(Guid.Empty, "mailcheck.tls.testname", MessageType.info, null, null);
            A.CallTo(() => _firstRule.TestCriteria).Returns(new TestCriteria());
            A.CallTo(() => _firstRule.Evaluate(A<TestContext>._, A<BouncyCastleTlsTestResult>._))
                .ReturnsLazily((TestContext context, BouncyCastleTlsTestResult _) =>
                {
                    context.Advisories.Add(firstRuleAdvisory);
                    return context.NextTest;
                });

            NamedAdvisory lastRuleAdvisory = new NamedAdvisory(Guid.Empty, "mailcheck.tls.testname", MessageType.info, null, null);
            A.CallTo(() => _lastRule.TestCriteria).Returns(new TestCriteria());
            A.CallTo(() => _lastRule.Evaluate(A<TestContext>._, A<BouncyCastleTlsTestResult>._))
                .ReturnsLazily((TestContext context, BouncyCastleTlsTestResult _) =>
                {
                    context.Advisories.Add(lastRuleAdvisory);
                    return context.NextTest;
                });

            SimplifiedTlsTestResults result = await _testRunner.Run("testIpAddress");

            Assert.AreEqual(2, result.AdvisoryMessages.Count);
            Assert.AreSame(firstRuleAdvisory, result.AdvisoryMessages[0]);
            Assert.AreSame(lastRuleAdvisory, result.AdvisoryMessages[1]);
        }

        [Test]
        public async Task RunNullsAdvisoriesIfInconclusive()
        {
            A.CallTo(() => _firstRule.TestCriteria).Returns(new TestCriteria { Name = "firstTestCriteria" });
            A.CallTo(() => _firstRule.Evaluate(A<TestContext>._, A<BouncyCastleTlsTestResult>._))
                .ReturnsLazily((TestContext context, BouncyCastleTlsTestResult _) =>
                {
                    context.Advisories.Add(new NamedAdvisory(Guid.Empty, "mailcheck.tls.testname", MessageType.info, null, null));
                    return context.NextTest;
                });

            A.CallTo(() => _lastRule.TestCriteria).Returns(new TestCriteria { Name = "secondTestCriteria" });
            A.CallTo(() => _lastRule.Evaluate(A<TestContext>._, A<BouncyCastleTlsTestResult>._))
                .ReturnsLazily((TestContext context, BouncyCastleTlsTestResult _) =>
                {
                    context.Inconclusive = true;
                    return context.NextTest; 
                });

            SimplifiedTlsTestResults result = await _testRunner.Run("testIpAddress");

            Assert.Null(result.AdvisoryMessages);
            Assert.True(result.Inconclusive);
        }
    }
}
