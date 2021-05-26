using MailCheck.Mx.TlsTester.Factory;
using MailCheck.Mx.TlsTester.MxTester;
using NUnit.Framework;

namespace MailCheck.Mx.TlsTester.Test.Factory
{
    [TestFixture]
    public class MxSecurityTesterFactoryTests
    {
        [Test]
        public void CanCreateTlsCompatibilityProcessor()
        {
            System.Environment.SetEnvironmentVariable("MxRecordLimit", "1");
            System.Environment.SetEnvironmentVariable("RefreshIntervalSeconds", "1");
            System.Environment.SetEnvironmentVariable("FailureRefreshIntervalSeconds", "1");
            System.Environment.SetEnvironmentVariable("TlsTestTimeoutSeconds", "1");
            System.Environment.SetEnvironmentVariable("BufferSize", "100");
            System.Environment.SetEnvironmentVariable("SlowResponseThresholdSeconds", "10");
            System.Environment.SetEnvironmentVariable("PrintStatsIntervalSeconds", "30");
            System.Environment.SetEnvironmentVariable("PublishBatchFlushIntervalSeconds", "10");
            System.Environment.SetEnvironmentVariable("PublishBatchSize", "10");
            System.Environment.SetEnvironmentVariable("TlsTesterThreadCount", "10");
            System.Environment.SetEnvironmentVariable("SmtpHostName", "localhost");
            System.Environment.SetEnvironmentVariable("CacheHostName", "localhost");
            System.Environment.SetEnvironmentVariable("SnsTopicArn", "localhost");
            System.Environment.SetEnvironmentVariable("SnsCertsTopicArn", "localhost");
            System.Environment.SetEnvironmentVariable("ConnectionString", "connectionString");
            System.Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", "AWS_SECRET_ACCESS_KEY");
            System.Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", "AWS_ACCESS_KEY_ID");
            System.Environment.SetEnvironmentVariable("AWS_SESSION_TOKEN", "AWS_SESSION_TOKEN");
            System.Environment.SetEnvironmentVariable("SqsQueueUrl", "SqsQueueUrl");
            System.Environment.SetEnvironmentVariable("TlsTesterHostRetestPeriodSeconds", "1");

            IMxSecurityTesterProcessor mxSecurityTesterProcessorRunner = MxSecurityTesterFactory.CreateMxSecurityTesterProcessor();
            Assert.That(mxSecurityTesterProcessorRunner, Is.Not.Null);
        }
    }
}
