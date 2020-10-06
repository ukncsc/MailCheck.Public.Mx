using System;
using System.Threading.Tasks;
using FakeItEasy;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Tester;
using MailCheck.Mx.Contracts.TlsEvaluator;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace MailCheck.Mx.TlsEvaluator.Test
{
    [TestFixture]
    public class EvaluatorResultsTests
    {
        [Test]
        public async Task WhenAllTestAreTcpConnectionFailedShouldReturnOneError()
        {
            EvaluatorResult expectedResult = EvaluatorResult.INCONCLUSIVE;
            string expectedMessage =
                "We were unable to create a TLS connection with this server. This could be because the server does not support TLS "
                + "or because Mail Check servers have been blocked. We will keep trying to test TLS with this server, so please check back later or get in touch "
                + "if you think there's a problem.";

            var mxHostTlsResults = new TlsTestResults("abc.def.gov.uk", false, false, new BouncyCastleTlsTestResult(TlsError.TCP_CONNECTION_FAILED, "", null),
                new BouncyCastleTlsTestResult(TlsError.TCP_CONNECTION_FAILED, "", null),
                new BouncyCastleTlsTestResult(TlsError.TCP_CONNECTION_FAILED, "", null),
                new BouncyCastleTlsTestResult(TlsError.TCP_CONNECTION_FAILED, "", null),
                new BouncyCastleTlsTestResult(TlsError.TCP_CONNECTION_FAILED, "", null),
                new BouncyCastleTlsTestResult(TlsError.TCP_CONNECTION_FAILED, "", null),
                new BouncyCastleTlsTestResult(TlsError.TCP_CONNECTION_FAILED, "", null),
                new BouncyCastleTlsTestResult(TlsError.TCP_CONNECTION_FAILED, "", null),
                new BouncyCastleTlsTestResult(TlsError.TCP_CONNECTION_FAILED, "", null),
                new BouncyCastleTlsTestResult(TlsError.TCP_CONNECTION_FAILED, "", null),
                new BouncyCastleTlsTestResult(TlsError.TCP_CONNECTION_FAILED, "", null),
                new BouncyCastleTlsTestResult(TlsError.TCP_CONNECTION_FAILED, "", null), null);

            IMxSecurityEvaluator mxSecurityEvaluator = A.Fake<IMxSecurityEvaluator>();
            ILogger<EvaluationProcessor> log = A.Fake<ILogger<EvaluationProcessor>>();

            IEvaluationProcessor processor = new EvaluationProcessor(mxSecurityEvaluator, log);
           TlsResultsEvaluated results = await  processor.Process(mxHostTlsResults);
            
            
            Assert.AreEqual(expectedResult, results.TlsRecords.Tls12AvailableWithBestCipherSuiteSelected.TlsEvaluatedResult.Result.Value);
            Assert.AreEqual(expectedMessage, results.TlsRecords.Tls12AvailableWithBestCipherSuiteSelected.TlsEvaluatedResult.Description);
        }
    }
}
