namespace MailCheck.Mx.Contracts.SharedDomain
{
    public enum EvaluatorResult
    {
        UNKNOWN = -1,
        PASS = 0,
        PENDING = 1,
        INCONCLUSIVE = 2,
        WARNING = 3,
        FAIL = 4,
        INFORMATIONAL = 5
    }
}