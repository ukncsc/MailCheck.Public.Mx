using MailCheck.Common.Environment.Abstractions;

namespace MailCheck.Mx.TlsEntity.Config
{
    public interface ITlsEntityConfig
    {
        int TlsResultsCacheInSeconds { get; }
        string SnsTopicArn { get; }
        int MaxTlsRetryAttempts { get; }
        int FailureNextScheduledInSeconds { get; }
        int NextScheduledInSeconds { get; }
        int MinimumSchedulerInterval { get; }
        string RecordType { get; }
        string WebUrl { get; }
        string SimplifiedRecordType { get; }
    }

    public class TlsEntityConfig : ITlsEntityConfig
    {
        public TlsEntityConfig(IEnvironmentVariables environmentVariables)
        {
            SnsTopicArn = environmentVariables.Get("SnsTopicArn");
            MaxTlsRetryAttempts = environmentVariables.GetAsInt("MaxTlsRetryAttempts");
            FailureNextScheduledInSeconds = environmentVariables.GetAsInt("FailureNextScheduledInSeconds");
            NextScheduledInSeconds = environmentVariables.GetAsInt("NextScheduledInSeconds");
            TlsResultsCacheInSeconds = environmentVariables.GetAsInt("TlsResultsCacheInSeconds");
            MinimumSchedulerInterval = environmentVariables.GetAsInt("MinimumSchedulerInterval");
            RecordType = "TLS";
            WebUrl = environmentVariables.Get("WebUrl");
            SimplifiedRecordType = "SIMPLIFIEDTLS";
        }

        public int TlsResultsCacheInSeconds { get; }
        public int NextScheduledInSeconds { get; }
        public int MinimumSchedulerInterval { get; }
        public int FailureNextScheduledInSeconds { get; }
        public string SnsTopicArn { get; }
        public int MaxTlsRetryAttempts { get; }
        public string RecordType { get; }
        public string WebUrl { get; }
        public string SimplifiedRecordType { get; }
    }
}
