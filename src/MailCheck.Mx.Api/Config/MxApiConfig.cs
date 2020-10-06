using MailCheck.Common.Environment.Abstractions;

namespace MailCheck.Mx.Api.Config
{
    public interface IMxApiConfig
    {
        string MicroserviceOutputSnsTopicArn { get; }
    }

    public class MxApiConfig : IMxApiConfig
    {
        public MxApiConfig(IEnvironmentVariables environmentVariables)
        {
            MicroserviceOutputSnsTopicArn = environmentVariables.Get("MicroserviceOutputSnsTopicArn");
        }
        
        public string MicroserviceOutputSnsTopicArn { get; }
    }
}
