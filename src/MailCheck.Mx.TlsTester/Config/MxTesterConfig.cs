using MailCheck.Common.Environment.Abstractions;
using System;

namespace MailCheck.Mx.TlsTester.Config
{
    public interface IMxTesterConfig
    {
        string SnsTopicArn { get; }
        string SqsQueueUrl { get; }
        string SmtpHostName { get; }

        /// <summary>
        /// Batch up results before publishing into batches of this size.
        /// </summary>
        int PublishBatchSize { get; }

        /// <summary>
        /// Batch up results before publishing but if no new results arrive during the flush 
        /// interval go ahead and publish the batch even if it's not a full batch.
        /// </summary>
        int PublishBatchFlushIntervalSeconds { get; }

        int PrintStatsIntervalSeconds { get; }

        /// <summary>
        /// Number of testers instances to create - each will perform tests on a different thread (managed by TPL).
        /// </summary>
        int TlsTesterThreadCount { get; }
    }

    public class MxTesterConfig : IMxTesterConfig
    {
        public MxTesterConfig(IEnvironmentVariables environmentVariables)
        {
            SnsTopicArn = environmentVariables.Get("SnsTopicArn");
            SqsQueueUrl = environmentVariables.Get("SqsQueueUrl");
            SmtpHostName = environmentVariables.Get("SmtpHostName");
            PublishBatchFlushIntervalSeconds = 30;
            PublishBatchSize = 10;
            PrintStatsIntervalSeconds = 60;
            TlsTesterThreadCount= 10;
        }

        public string SnsTopicArn { get; }
        public string SmtpHostName { get; }
        public int PublishBatchFlushIntervalSeconds { get; }
        public int PublishBatchSize { get; }
        public string SqsQueueUrl { get; }
        public int PrintStatsIntervalSeconds { get; }
        public int TlsTesterThreadCount { get; }
    }
}
