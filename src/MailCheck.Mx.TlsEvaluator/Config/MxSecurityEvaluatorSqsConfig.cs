using MailCheck.Common.Environment.Abstractions;

namespace MailCheck.Mx.TlsEvaluator.Config
{
    public interface ITlsRptEvaluatorConfig
    {
        string SnsTopicArn { get; }
        string QueueUrl { get; }
        int MaxNumberOfMessages { get; }
        int WaitTimeSeconds { get; }
    }

    public class TlsRptEvaluatorConfig : ITlsRptEvaluatorConfig
    {
        public TlsRptEvaluatorConfig(IEnvironmentVariables environmentVariables)
        {
            SnsTopicArn = environmentVariables.Get("SnsTopicArn");
            MaxNumberOfMessages = 1;
            QueueUrl = environmentVariables.Get("SqsQueueUrl");
            WaitTimeSeconds = 20;
        }

        public int MaxNumberOfMessages { get; }
        public string QueueUrl { get; }
        public int WaitTimeSeconds { get; }

        public string SnsTopicArn { get; }
    }
}

