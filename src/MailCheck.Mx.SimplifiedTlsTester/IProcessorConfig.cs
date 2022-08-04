using System;

namespace MailCheck.Mx.SimplifiedTlsTester
{
    public interface IProcessorConfig
    {
        string SnsTopicArn { get; }
        string SqsQueueUrl { get; }
        TimeSpan TestRunTimeout { get; }
    }
}