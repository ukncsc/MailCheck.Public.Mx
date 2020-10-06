using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Tester;
using MailCheck.Mx.Contracts.TlsEvaluator;
using MailCheck.Mx.TlsEvaluator.Mapping;
using MailCheck.Mx.TlsEvaluator.Rules.TlsEvaluation;
using MailCheck.Mx.TlsEvaluator.Util;

namespace MailCheck.Mx.TlsEvaluator
{
    public interface IMxSecurityEvaluator
    {
        Task<TlsResultsEvaluated> Evaluate(TlsTestResults tlsTestResults);
    }

    public class MxSecurityEvaluator : IMxSecurityEvaluator
    {
        private readonly IEvaluator<TlsTestResults, RuleTypedTlsEvaluationResult> _evaluator;

        public MxSecurityEvaluator(IEvaluator<TlsTestResults, RuleTypedTlsEvaluationResult> evaluator)
        {
            _evaluator = evaluator;
        }

        public async Task<TlsResultsEvaluated> Evaluate(TlsTestResults tlsTestResults)
        {
            EvaluationResult<TlsTestResults, RuleTypedTlsEvaluationResult> evaluationResult =
                await _evaluator.Evaluate(tlsTestResults, tlsEvaluatedResult =>
                    tlsEvaluatedResult.Any(_ => _.TlsEvaluatedResult.Result != EvaluatorResult.PASS));

            Dictionary<TlsTestType, RuleTypedTlsEvaluationResult> evaluationResultsByType =
                evaluationResult.Messages.ToDictionary(_ => _.Type);

            return new TlsResultsEvaluated(tlsTestResults.Id, tlsTestResults.Failed, new TlsRecords(
                new TlsRecord(
                    GetEvaluatorResultOrDefault(TlsTestType.Tls12AvailableWithBestCipherSuiteSelected,
                        evaluationResultsByType), tlsTestResults.Tls12AvailableWithBestCipherSuiteSelected),
                new TlsRecord(
                    GetEvaluatorResultOrDefault(TlsTestType.Tls12AvailableWithBestCipherSuiteSelectedFromReverseList,
                        evaluationResultsByType),
                    tlsTestResults.Tls12AvailableWithBestCipherSuiteSelectedFromReverseList),
                new TlsRecord(
                    GetEvaluatorResultOrDefault(TlsTestType.Tls12AvailableWithSha2HashFunctionSelected,
                        evaluationResultsByType), tlsTestResults.Tls12AvailableWithSha2HashFunctionSelected),
                new TlsRecord(
                    GetEvaluatorResultOrDefault(TlsTestType.Tls12AvailableWithWeakCipherSuiteNotSelected,
                        evaluationResultsByType), tlsTestResults.Tls12AvailableWithWeakCipherSuiteNotSelected),
                new TlsRecord(
                    GetEvaluatorResultOrDefault(TlsTestType.Tls11AvailableWithBestCipherSuiteSelected,
                        evaluationResultsByType), tlsTestResults.Tls11AvailableWithBestCipherSuiteSelected),
                new TlsRecord(
                    GetEvaluatorResultOrDefault(TlsTestType.Tls11AvailableWithWeakCipherSuiteNotSelected,
                        evaluationResultsByType), tlsTestResults.Tls11AvailableWithWeakCipherSuiteNotSelected),
                new TlsRecord(
                    GetEvaluatorResultOrDefault(TlsTestType.Tls10AvailableWithBestCipherSuiteSelected,
                        evaluationResultsByType), tlsTestResults.Tls10AvailableWithBestCipherSuiteSelected),
                new TlsRecord(
                    GetEvaluatorResultOrDefault(TlsTestType.Tls10AvailableWithWeakCipherSuiteNotSelected,
                        evaluationResultsByType), tlsTestResults.Tls10AvailableWithWeakCipherSuiteNotSelected),
                new TlsRecord(
                    GetEvaluatorResultOrDefault(TlsTestType.Ssl3FailsWithBadCipherSuite, evaluationResultsByType),
                    tlsTestResults.Ssl3FailsWithBadCipherSuite),
                new TlsRecord(
                    GetEvaluatorResultOrDefault(TlsTestType.TlsSecureEllipticCurveSelected, evaluationResultsByType),
                    tlsTestResults.TlsSecureEllipticCurveSelected),
                new TlsRecord(
                    GetEvaluatorResultOrDefault(TlsTestType.TlsSecureDiffieHellmanGroupSelected,
                        evaluationResultsByType), tlsTestResults.TlsSecureDiffieHellmanGroupSelected),
                new TlsRecord(
                    GetEvaluatorResultOrDefault(TlsTestType.TlsWeakCipherSuitesRejected, evaluationResultsByType),
                    tlsTestResults.TlsWeakCipherSuitesRejected),
                new TlsRecord(GetEvaluatorResultOrDefault(TlsTestType.Tls12Available, evaluationResultsByType)),
                new TlsRecord(GetEvaluatorResultOrDefault(TlsTestType.Tls11Available, evaluationResultsByType)),
                new TlsRecord(GetEvaluatorResultOrDefault(TlsTestType.Tls10Available, evaluationResultsByType))));
        }

        private TlsEvaluatedResult GetEvaluatorResultOrDefault(TlsTestType tlsTestType, Dictionary<TlsTestType, RuleTypedTlsEvaluationResult> evaluationResultsByType)
        {
            return evaluationResultsByType.TryGetValue(tlsTestType, out RuleTypedTlsEvaluationResult ruleTypedTlsEvaluationResult)
                ? ruleTypedTlsEvaluationResult.TlsEvaluatedResult
                : new TlsEvaluatedResult(Guid.NewGuid());
        }
    }
}
