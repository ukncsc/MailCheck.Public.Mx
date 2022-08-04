using System;
using MailCheck.Common.Environment.Abstractions;
using MailCheck.Mx.BouncyCastle.Config;
using MailCheck.Mx.SimplifiedTlsTester.Smtp;

namespace MailCheck.Mx.SimplifiedTlsTester
{
    public class HostedConfig : IProcessorConfig, ISmtpClientConfig, IBouncyCastleClientConfig
    {
        public HostedConfig(IEnvironmentVariables environmentVariables)
        {
            SnsTopicArn = environmentVariables.Get("SnsTopicArn");
            SqsQueueUrl = environmentVariables.Get("SqsQueueUrl");
            SmtpHostNameSuffix = environmentVariables.Get("SmtpHostNameSuffix");
            TcpSendReceiveTimeout = TimeSpan.FromSeconds(environmentVariables.GetAsInt("TcpSendReceiveTimeoutSeconds"));
            TcpConnectionTimeout = TimeSpan.FromSeconds(environmentVariables.GetAsInt("TcpConnectionTimeoutSeconds"));
            TestRunTimeout = TimeSpan.FromSeconds(environmentVariables.GetAsInt("TestRunTimeoutSeconds"));
        }

        public string SnsTopicArn { get; }
        public string SmtpHostNameSuffix { get; }
        public string SqsQueueUrl { get; }
        public TimeSpan TcpSendReceiveTimeout { get; }
        public TimeSpan TcpConnectionTimeout { get; }
        public TimeSpan TestRunTimeout { get; }
    }
}