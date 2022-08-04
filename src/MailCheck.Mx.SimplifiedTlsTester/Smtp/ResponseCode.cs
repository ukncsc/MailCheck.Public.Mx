namespace MailCheck.Mx.SimplifiedTlsTester.Smtp
{
    public enum ResponseCode
    {
        ServiceReady = 220,
        Ok = 250,
        TransientError = 421,
        Unknown = -1
    }
}