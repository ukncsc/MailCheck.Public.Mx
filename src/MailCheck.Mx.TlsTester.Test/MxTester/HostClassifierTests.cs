using System;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using MailCheck.Mx.Contracts.Tester;
using MailCheck.Mx.TlsTester.Config;
using MailCheck.Mx.TlsTester.Domain;
using MailCheck.Mx.TlsTester.MxTester;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace MailCheck.Mx.TlsTester.Test.MxTester
{
    [TestFixture]
    public class HostClassifierTests
    {
        private ITlsSecurityTesterAdapator _tester;
        private Func<TimeSpan, TlsTestResults, CancellationToken, Task<TlsTestResults>> _delayFunc;
        private HostClassifier _classifier;

        [SetUp]
        public void SetUp()
        {
            _tester = A.Fake<ITlsSecurityTesterAdapator>();
            _delayFunc = A.Fake<Func<TimeSpan, TlsTestResults, CancellationToken, Task<TlsTestResults>>>();
            _classifier = new HostClassifier(
                _tester,
                A.Fake<IMxTesterConfig>(),
                A.Fake<ILogger<HostClassifier>>(),
                _delayFunc);
        }

        [Test]
        public async Task FastHosts()
        {
            TlsTestPending pendingTest = new TlsTestPending("host.com");
            TlsTestResults tlsTestResults = CreateMxHostTestResult();

            A.CallTo(() => _tester.Test(pendingTest, A<int[]>._)).Returns(tlsTestResults);
            A.CallTo(() => _delayFunc(A<TimeSpan>._, A<TlsTestResults>._, A<CancellationToken>._)).Returns(TaskHelpers.NeverReturn<TlsTestResults>());

            ClassificationResult classificationResult = await _classifier.Classify(pendingTest);

            Assert.That(classificationResult, Is.Not.Null);
            Assert.That(classificationResult.Classification, Is.EqualTo(Classifications.Fast));
        }

        [Test]
        public async Task SlowHosts()
        {
            TlsTestPending pendingTest = new TlsTestPending("host.com");

            A.CallTo(() => _tester.Test(pendingTest, A<int[]>._)).Returns(TaskHelpers.NeverReturn<TlsTestResults>());
            A.CallTo(() => _delayFunc(A<TimeSpan>._, A<TlsTestResults>._, A<CancellationToken>._)).Returns(HostClassifier.TimeoutResult);

            ClassificationResult classificationResult = await _classifier.Classify(pendingTest);

            Assert.That(classificationResult, Is.Not.Null);
            Assert.That(classificationResult.Classification, Is.EqualTo(Classifications.Slow));
        }

        private TlsTestResults CreateMxHostTestResult()
        {
            return new TlsTestResults(
                "host.abc.gov.uk",
                false,
                false,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null
            );
        }
    }
}
