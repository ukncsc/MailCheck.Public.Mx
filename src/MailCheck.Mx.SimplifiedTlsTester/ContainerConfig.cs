using System;
using System.IO;
using System.Net;
using Microsoft.Extensions.Logging;

namespace MailCheck.Mx.SimplifiedTlsTester
{
    public interface IContainerConfig
    {
        public void LogContainerDetails(ILogger log);
    }

    public class ContainerConfig : IContainerConfig
    {
        public string ContainerMetadata { get; }
        public void LogContainerDetails(ILogger log)
        {
            log.LogInformation($"Container metadata: {ContainerMetadata}");
        }

        public ContainerConfig()
        {
            ContainerMetadata = GetMetadataFromEndpoint();
        }

        public static string GetMetadataFromEndpoint()
        {
            try
            {
                string endpointUrl = "http://169.254.170.2/v2/metadata";

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(endpointUrl);
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    string metadata = reader.ReadToEnd();
                    return metadata;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}