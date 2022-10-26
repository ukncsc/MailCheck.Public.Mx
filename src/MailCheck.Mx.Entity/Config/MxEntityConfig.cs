using MailCheck.Common.Environment.Abstractions;

namespace MailCheck.Mx.Entity.Config
{
    public interface IMxEntityConfig
    {
        string SnsTopicArn { get; }
        int NextScheduledInSeconds { get; }
        string WebUrl { get; }
    }

    public class MxEntityConfig : IMxEntityConfig
    {
        public MxEntityConfig(IEnvironmentVariables environmentVariables)
        {
            SnsTopicArn = environmentVariables.Get("SnsTopicArn");
            NextScheduledInSeconds = environmentVariables.GetAsInt("NextScheduledInSeconds");
            WebUrl = environmentVariables.Get("WebUrl");
        }

        public string SnsTopicArn { get; }
        public int NextScheduledInSeconds { get; }
        public string WebUrl { get; }
    }
}
