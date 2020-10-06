using System;
using MailCheck.Common.Environment.Abstractions;

namespace MailCheck.Mx.Entity.Config
{
    public interface IMxEntityConfig
    {
        string SnsTopicArn { get; }
        int NextScheduledInSeconds { get; }
    }

    public class MxEntityConfig : IMxEntityConfig
    {
        public MxEntityConfig(IEnvironmentVariables environmentVariables)
        {
            SnsTopicArn = environmentVariables.Get("SnsTopicArn");
            NextScheduledInSeconds = environmentVariables.GetAsInt("NextScheduledInSeconds");
        }

        public string SnsTopicArn { get; }
        public int NextScheduledInSeconds { get; }
    }
}
