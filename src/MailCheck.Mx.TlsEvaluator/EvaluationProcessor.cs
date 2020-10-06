using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Tester;
using MailCheck.Mx.Contracts.TlsEvaluator;
using MailCheck.Mx.TlsEvaluator.Mapping;
using Microsoft.Extensions.Logging;

namespace MailCheck.Mx.TlsEvaluator
{
    public interface IEvaluationProcessor
    {
        Task<TlsResultsEvaluated> Process(TlsTestResults testTestResults);
    }

    public class EvaluationProcessor : IEvaluationProcessor
    {
        private readonly ILogger<EvaluationProcessor> _log;
        private readonly IMxSecurityEvaluator _mxSecurityEvaluator;

        public EvaluationProcessor(
            IMxSecurityEvaluator mxSecurityEvaluator,
            ILogger<EvaluationProcessor> log)
        {
            _mxSecurityEvaluator = mxSecurityEvaluator;
            _log = log;
        }

        public async Task<TlsResultsEvaluated> Process(TlsTestResults tlsTestRptRecords)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            TlsResultsEvaluated result =  await EvaluateMxRecordProfile(tlsTestRptRecords);
            stopwatch.Stop();
            _log.LogDebug($"Processed domain with ID {tlsTestRptRecords.Id}. Took {stopwatch.Elapsed.TotalSeconds} seconds.");
            return result;
        }

        protected async Task<TlsResultsEvaluated> EvaluateMxRecordProfile(TlsTestResults tlsTestResults)
        {
            List<BouncyCastleTlsTestResult> bouncyCastleTlsTestResults = new List<BouncyCastleTlsTestResult> {
                tlsTestResults.Ssl3FailsWithBadCipherSuite,
                tlsTestResults.Tls10AvailableWithBestCipherSuiteSelected,
                tlsTestResults.Tls10AvailableWithWeakCipherSuiteNotSelected,
                tlsTestResults.Tls11AvailableWithBestCipherSuiteSelected,
                tlsTestResults.Tls11AvailableWithWeakCipherSuiteNotSelected,
                tlsTestResults.Tls12AvailableWithBestCipherSuiteSelected,
                tlsTestResults.Tls12AvailableWithBestCipherSuiteSelectedFromReverseList,
                tlsTestResults.Tls12AvailableWithSha2HashFunctionSelected,
                tlsTestResults.Tls12AvailableWithWeakCipherSuiteNotSelected,
                tlsTestResults.TlsSecureDiffieHellmanGroupSelected,
                tlsTestResults.TlsSecureEllipticCurveSelected,
                tlsTestResults.TlsWeakCipherSuitesRejected
            };

            bool hasFailedConnection = bouncyCastleTlsTestResults.All(_ =>
                _.TlsError == TlsError.SESSION_INITIALIZATION_FAILED ||
                _.TlsError == TlsError.TCP_CONNECTION_FAILED);

            if (hasFailedConnection)
            {
                _log.LogDebug($"TLS connection failed for host {tlsTestResults.Id}");

                string failedConnectionErrors = string.Join(", ", bouncyCastleTlsTestResults
                    .Select(_ => _.ErrorDescription)
                    .Distinct()
                    .ToList()); 

                return GetConnectionFailedResults(tlsTestResults.Id, failedConnectionErrors, tlsTestResults.ToTlsResult());
            }

            bool hostNotFound = bouncyCastleTlsTestResults.All(_ => _.TlsError == TlsError.HOST_NOT_FOUND);

            if (hostNotFound)
            {
                _log.LogDebug($"Host not found for {tlsTestResults.Id}");

                return GetHostNotFoundResults(tlsTestResults.Id, tlsTestResults.ToTlsResult());
            }

            _log.LogDebug($"Evaluating TLS connection results for {tlsTestResults.Id}.");

            TlsResultsEvaluated tlsEvaluatedResults = await _mxSecurityEvaluator.Evaluate(tlsTestResults);
            return tlsEvaluatedResults;
        }

        public static Guid ErrorId1 => Guid.Parse("CBB3E337-E7D2-480A-8310-5B13752FC2F9");
        public static Guid ErrorId2 => Guid.Parse("E9C5CACD-9B41-440C-8A80-A610B338B620");

        public static TlsResultsEvaluated GetConnectionFailedResults(string hostname, string errorDescription,
            TlsResults tlsResults)
        {
            string errorMessage =
                "We were unable to create a TLS connection with this server. This could be because the server does not support " +
                "TLS or because Mail Check servers have been blocked. We will keep trying to test TLS with this server, " +
                $"so please check back later or get in touch if you think there's a problem.";

            if (!string.IsNullOrWhiteSpace(errorDescription))
            {
                errorMessage += $" Error description \"{errorDescription}\".";
            }

            TlsEvaluatedResult tlsEvaluatedResult = new TlsEvaluatedResult(ErrorId1, EvaluatorResult.INCONCLUSIVE, errorMessage);

            return CreateSingleTlsResult(hostname, tlsEvaluatedResult, tlsResults);
        }

        public static TlsResultsEvaluated GetHostNotFoundResults(string hostname, TlsResults tlsResults)
        {
            TlsEvaluatedResult tlsEvaluatedResult = new TlsEvaluatedResult(ErrorId2, EvaluatorResult.FAIL,
                $"This hostname {hostname} does not exist.");

            return CreateSingleTlsResult(hostname, tlsEvaluatedResult, tlsResults);
        }

        private static TlsResultsEvaluated CreateSingleTlsResult(string host, TlsEvaluatedResult tlsEvaluatedResult,
            TlsResults tlsResults) =>
            new TlsResultsEvaluated(host, tlsResults.Failed,
                new TlsRecords(new TlsRecord(tlsEvaluatedResult, tlsResults.Tls12AvailableWithBestCipherSuiteSelected),
                    new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                    new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                    new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                    new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                    new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                    new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                    new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                    new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                    new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                    new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                    new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                    new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                    new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS)),
                    new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS))));
    }
}
