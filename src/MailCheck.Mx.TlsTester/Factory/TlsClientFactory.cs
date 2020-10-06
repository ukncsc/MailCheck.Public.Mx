using System;
using MailCheck.Common.Environment.Abstractions;
using MailCheck.Common.Environment.Implementations;
using MailCheck.Common.Logging;
using MailCheck.Mx.BouncyCastle;
using MailCheck.Mx.BouncyCastle.Config;
using MailCheck.Mx.TlsTester.Config;
using MailCheck.Mx.TlsTester.Smtp;
using MailCheck.Mx.TlsTester.Tls;
using Microsoft.Extensions.DependencyInjection;

namespace MailCheck.Mx.TlsTester.Factory
{
    public interface ITlsClientFactory
    {
        ITlsClient Create();
    }

    public class TlsClientFactory : ITlsClientFactory
    {
        public ITlsClient Create()
        {
            IServiceProvider serviceProvider = new ServiceCollection()
                .AddTransient<ITlsClient, SmtpTlsClient>()
                .AddTransient<ISmtpClient, SmtpClient>()
                .AddTransient<IMxTesterConfig, MxTesterConfig>()
                .AddTransient<IBouncyCastleClientConfig, BouncyCastleClientConfig>()
                .AddTransient<ISmtpSerializer, SmtpSerializer>()
                .AddTransient<ISmtpDeserializer, SmtpDeserializer>()
                .AddTransient<IEnvironmentVariables, EnvironmentVariables>()
                .AddTransient<IEnvironment, EnvironmentWrapper>()
                .AddSerilogLogging()
                .BuildServiceProvider();

            return serviceProvider.GetService<ITlsClient>();
        }
    }
}
