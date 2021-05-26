using System;
using MailCheck.Common.Environment.Abstractions;

namespace MailCheck.Mx.Poller.Config
{
    public interface IMxPollerConfig
    {
        string SnsTopicArn { get; }
        TimeSpan DnsRecordLookupTimeout { get; }
        string NameServer { get; }
    }

    public class MxPollerConfig : IMxPollerConfig
    {
        public MxPollerConfig(IEnvironmentVariables environmentVariables)
        {
            SnsTopicArn = environmentVariables.Get("SnsTopicArn");
            DnsRecordLookupTimeout = TimeSpan.FromSeconds(environmentVariables.GetAsLong("DnsRecordLookupTimeoutSeconds"));
            NameServer = environmentVariables.Get("NameServer", false);
        }

        public string SnsTopicArn { get; }
        public TimeSpan DnsRecordLookupTimeout { get; }
        public string NameServer { get; }
    }
}
