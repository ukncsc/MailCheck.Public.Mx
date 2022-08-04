using MailCheck.Common.Environment.Abstractions;

namespace MailCheck.Mx.Api.Config
{
    public interface IMxApiConfig
    {
        string MicroserviceOutputSnsTopicArn { get; }
        int RecheckMinPeriodInSeconds { get; }
        string SnsTopicArn { get; }
    }

    public class MxApiConfig : IMxApiConfig
    {
        public MxApiConfig(IEnvironmentVariables environmentVariables)
        {
            MicroserviceOutputSnsTopicArn = environmentVariables.Get("MicroserviceOutputSnsTopicArn");
            RecheckMinPeriodInSeconds = environmentVariables.GetAsInt("RecheckMinPeriodInSeconds");
            SnsTopicArn = environmentVariables.Get("SnsTopicArn");
        }
        
        public string MicroserviceOutputSnsTopicArn { get; }
        public int RecheckMinPeriodInSeconds { get; }
        public string SnsTopicArn { get; }
    }
}
