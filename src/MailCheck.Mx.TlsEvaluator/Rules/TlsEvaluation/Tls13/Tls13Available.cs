using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Tester;
using MailCheck.Mx.TlsEvaluator.Util;

namespace MailCheck.Mx.TlsEvaluator.Rules.TlsEvaluation.Tls13
{
    public class Tls13Available : IRule<TlsTestResults, RuleTypedTlsEvaluationResult>
    {
        public static readonly Guid Tls13AvailableId = new Guid("7bf64d2e-db70-406b-8c77-434e33c8efd2");
        public static readonly Guid Tls13UnavailableId = new Guid("1a99882d-a685-4989-a68e-a50c9627cbf4");

        public Task<List<RuleTypedTlsEvaluationResult>> Evaluate(TlsTestResults tlsTestConnectionResults)
        {
            BouncyCastleTlsTestResult tls13Available = tlsTestConnectionResults.Tls13AvailableWithBestCipherSuiteSelected;

            TlsTestType tlsTestType = TlsTestType.Tls13Available;

            if (tls13Available.Supported())
            {
                return new RuleTypedTlsEvaluationResult(tlsTestType, Guid.NewGuid(), EvaluatorResult.PASS).ToTaskList();
            }

            return new RuleTypedTlsEvaluationResult(tlsTestType, Tls13UnavailableId, EvaluatorResult.INFORMATIONAL, 
                "This server does not support TLS 1.3").ToTaskList();
        }

        public int SequenceNo => 1;
        public bool IsStopRule => true;
        public string Category => RuleCategory.Tls13;
    }
}
