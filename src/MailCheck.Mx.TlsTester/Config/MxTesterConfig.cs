using MailCheck.Common.Environment.Abstractions;
using System;

namespace MailCheck.Mx.TlsTester.Config
{
    public interface IMxTesterConfig
    {
        string SnsTopicArn { get; }
        string SqsQueueUrl { get; }
        string SmtpHostName { get; }

        int BufferSize { get; }

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
        /// Threshold in seconds at which the test completion time for a host is considered 
        /// slow and will be pushed to the slow lane
        /// </summary>
        int SlowResponseThresholdSeconds { get; }

        /// <summary>
        /// Number of testers instances to create - each will perform tests on a different thread (managed by TPL).
        /// </summary>
        int TlsTesterThreadCount { get; }


        int TlsTesterHostRetestPeriodSeconds { get; }
    }

    public class MxTesterConfig : IMxTesterConfig
    {
        public MxTesterConfig(IEnvironmentVariables environmentVariables)
        {
            SnsTopicArn = environmentVariables.Get("SnsTopicArn");
            SqsQueueUrl = environmentVariables.Get("SqsQueueUrl");
            SmtpHostName = environmentVariables.Get("SmtpHostName");
            BufferSize = environmentVariables.GetAsInt("BufferSize");
            PublishBatchFlushIntervalSeconds = environmentVariables.GetAsInt("PublishBatchFlushIntervalSeconds");
            PublishBatchSize = environmentVariables.GetAsInt("PublishBatchSize");
            PrintStatsIntervalSeconds = environmentVariables.GetAsInt("PrintStatsIntervalSeconds");
            SlowResponseThresholdSeconds = environmentVariables.GetAsInt("SlowResponseThresholdSeconds");
            TlsTesterHostRetestPeriodSeconds = environmentVariables.GetAsInt("TlsTesterHostRetestPeriodSeconds");
            TlsTesterThreadCount = environmentVariables.GetAsInt("TlsTesterThreadCount");
        }

        public string SnsTopicArn { get; }
        public string SmtpHostName { get; }
        public string SqsQueueUrl { get; }
        public int BufferSize { get; }
        public int RefreshIntervalSeconds { get; }
        public int PublishBatchFlushIntervalSeconds { get; }
        public int PublishBatchSize { get; }
        public int PrintStatsIntervalSeconds { get; }
        public int SlowResponseThresholdSeconds { get; }
        public int TlsTesterThreadCount { get; }
        public int TlsTesterHostRetestPeriodSeconds { get; }
    }
}
