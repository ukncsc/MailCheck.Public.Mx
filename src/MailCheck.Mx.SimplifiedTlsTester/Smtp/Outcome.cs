namespace MailCheck.Mx.SimplifiedTlsTester.Smtp
{
    public enum Outcome
    {
        Exception,
        NoResponse,
        NotReady,
        Ready,
        StartTlsNotSupported,
        StartTlsRequestFailed,
        TransientError
    }
}