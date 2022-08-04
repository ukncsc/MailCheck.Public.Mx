using System;
using MailCheck.Mx.BouncyCastle;
using MailCheck.Mx.BouncyCastle.Config;
using MailCheck.Mx.SimplifiedTlsTester.Smtp;
using Microsoft.Extensions.DependencyInjection;

namespace MailCheck.Mx.SimplifiedTlsTester.TlsClient
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddTlsClientFactory<TConfig>(this IServiceCollection serviceCollection)
            where TConfig : class, ISmtpClientConfig, IBouncyCastleClientConfig
        {
            return serviceCollection
                // ITlsClient is disposable so we add a factory function...
                .AddSingleton<Func<ITlsClient>>(provider => () => provider.GetService<ITlsClient>())
                .AddTransient<ITlsClient, SmtpTlsClient>()
                .AddTransient<ISmtpClient, SmtpClient>()
                .AddTransient<ISmtpClientConfig, TConfig>()
                .AddTransient<IBouncyCastleClientConfig, TConfig>()
                .AddTransient<ISmtpSerializer, SmtpSerializer>()
                .AddTransient<ISmtpDeserializer, SmtpDeserializer>();
        }
    }
}
