using System.Collections.Generic;
using System.Threading.Tasks;
using MailCheck.Mx.Contracts.SharedDomain;

namespace MailCheck.Mx.TlsEvaluator.Rules.TlsEvaluation
{
    public static class BouncyCastleTlsTestResultExtensions
    {
        public static Task<List<RuleTypedTlsEvaluationResult>> ToTaskList(this RuleTypedTlsEvaluationResult tlsEvaluatedResult)
        {
            return Task.FromResult(new List<RuleTypedTlsEvaluationResult> { tlsEvaluatedResult });
        }

        public static bool IsInconclusive(this BouncyCastleTlsTestResult bouncyCastleTlsTestResult)
        {
            return bouncyCastleTlsTestResult.TlsError == TlsError.TCP_CONNECTION_FAILED ||
                   bouncyCastleTlsTestResult.TlsError == TlsError.SESSION_INITIALIZATION_FAILED;
        }

        public static bool Supported(this BouncyCastleTlsTestResult bouncyCastleTlsTestResult)
        {
            return bouncyCastleTlsTestResult.TlsError == null;
        }

        public static bool ExplicitlyUnsupported(this BouncyCastleTlsTestResult bouncyCastleTlsTestResult)
        {
            return bouncyCastleTlsTestResult.TlsError == TlsError.PROTOCOL_VERSION;
        }
        
        public static bool HandshakeFailure(this BouncyCastleTlsTestResult bouncyCastleTlsTestResult)
        {
            return bouncyCastleTlsTestResult.TlsError == TlsError.HANDSHAKE_FAILURE;
        }

        public static bool InsufficientSecurity(this BouncyCastleTlsTestResult bouncyCastleTlsTestResult)
        {
            return bouncyCastleTlsTestResult.TlsError == TlsError.INSUFFICIENT_SECURITY;
        }
    }
}