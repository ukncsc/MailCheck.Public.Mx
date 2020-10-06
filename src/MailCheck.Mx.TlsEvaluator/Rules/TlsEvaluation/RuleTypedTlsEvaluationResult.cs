using System;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.TlsEvaluator.Util;

namespace MailCheck.Mx.TlsEvaluator.Rules.TlsEvaluation
{
    public class RuleTypedTlsEvaluationResult
    {
        public RuleTypedTlsEvaluationResult(TlsTestType type, Guid id, EvaluatorResult? result = null, string description = null)
            : this(type, new TlsEvaluatedResult(id, result, description))
        {}

        public RuleTypedTlsEvaluationResult(TlsTestType type, TlsEvaluatedResult tlsEvaluatedResult)
        {
            Type = type;
            TlsEvaluatedResult = tlsEvaluatedResult;
        }

        public TlsTestType Type { get; }
        public TlsEvaluatedResult TlsEvaluatedResult { get; }
    }
}