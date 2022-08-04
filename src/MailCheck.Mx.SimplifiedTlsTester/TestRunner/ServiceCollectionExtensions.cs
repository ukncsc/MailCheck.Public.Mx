using Microsoft.Extensions.DependencyInjection;
using MailCheck.Mx.BouncyCastle.Config;
using MailCheck.Mx.SimplifiedTlsTester.Smtp;
using MailCheck.Mx.SimplifiedTlsTester.TlsClient;

namespace MailCheck.Mx.SimplifiedTlsTester.TestRunner
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddTestRunner<TConfig>(this IServiceCollection serviceCollection)
            where TConfig : class, ISmtpClientConfig, IBouncyCastleClientConfig
        {
            return serviceCollection
                .AddTlsClientFactory<TConfig>()
                .AddSingleton<ITestRunner, TestRunner>()
                .AddSingleton<ITlsTester, TlsTester>();
        }
    }
}
