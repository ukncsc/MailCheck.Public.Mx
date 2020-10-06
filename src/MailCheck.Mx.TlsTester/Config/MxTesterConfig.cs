using MailCheck.Common.Environment.Abstractions;

namespace MailCheck.Mx.TlsTester.Config
{
    public interface IMxTesterConfig
    {
        string SnsTopicArn { get; }
        string SmtpHostName { get; }
        int PublishBatchFlushIntervalSeconds { get; }
        int SchedulerRunIntervalSeconds { get; }
        int PublishBatchSize { get; }
        string SqsQueueUrl { get; }
    }

    public class MxTesterConfig : IMxTesterConfig
    {
        public MxTesterConfig(IEnvironmentVariables environmentVariables)
        {
            SnsTopicArn = environmentVariables.Get("SnsTopicArn");
            SqsQueueUrl = environmentVariables.Get("SqsQueueUrl");
            SmtpHostName = environmentVariables.Get("SmtpHostName");
            SchedulerRunIntervalSeconds = environmentVariables.GetAsInt("SchedulerRunIntervalSeconds");
            PublishBatchFlushIntervalSeconds = 2;
            PublishBatchSize = 10;
        }

        public string SnsTopicArn { get; }
        public string SmtpHostName { get; }
        public int PublishBatchFlushIntervalSeconds { get; }
        public int SchedulerRunIntervalSeconds { get; }
        public int PublishBatchSize { get; }
        public string SqsQueueUrl { get; }
    }
}
