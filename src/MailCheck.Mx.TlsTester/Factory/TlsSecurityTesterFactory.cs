using System;
using MailCheck.Common.Logging;
using MailCheck.Common.Util;
using MailCheck.Mx.BouncyCastle;
using MailCheck.Mx.BouncyCastle.Config;
using MailCheck.Mx.TlsTester.Config;
using MailCheck.Mx.TlsTester.MxTester;
using MailCheck.Mx.TlsTester.Smtp;
using MailCheck.Mx.TlsTester.Tls;
using MailCheck.Mx.TlsTester.Tls.Tests;
using Microsoft.Extensions.DependencyInjection;

namespace MailCheck.Mx.TlsTester.Factory
{
    public class TlsSecurityTesterFactory
    {
        private static ServiceProvider Provider;
        
        public static ITlsSecurityTester CreateTester<TConfig>(TConfig config)
            where TConfig : IMxTesterConfig, IBouncyCastleClientConfig
        {
            Provider = new ServiceCollection()
                .AddTransient<ITlsSecurityTester, TlsSecurityTester>()
                .AddTransient<ITlsTest, Tls12AvailableWithBestCipherSuiteSelected>()
                .AddTransient<ITlsTest, Tls12AvailableWithBestCipherSuiteSelectedFromReversedList>()
                .AddTransient<ITlsTest, Tls12AvailableWithSha2HashFunctionSelected>()
                .AddTransient<ITlsTest, Tls12AvailableWithWeakCipherSuiteNotSelected>()
                .AddTransient<ITlsTest, Tls11AvailableWithBestCipherSuiteSelected>()
                .AddTransient<ITlsTest, Tls11AvailableWithWeakCipherSuiteNotSelected>()
                .AddTransient<ITlsTest, Tls10AvailableWithBestCipherSuiteSelected>()
                .AddTransient<ITlsTest, Tls10AvailableWithWeakCipherSuiteNotSelected>()
                .AddTransient<ITlsTest, Ssl3FailsWithBadCipherSuite>()
                .AddTransient<ITlsTest, TlsSecureEllipticCurveSelected>()
                .AddTransient<ITlsTest, TlsSecureDiffieHelmanGroupSelected>()
                .AddTransient<ITlsTest, TlsWeakCipherSuitesRejected>()
                .AddTransient<ITlsSecurityTesterAdapator, TlsSecurityTesterAdapator>()
                .AddTransient<IMxTesterConfig, MxTesterConfig>()
                .AddTransient<ITlsSecurityTesterAdapator, TlsSecurityTesterAdapator>()
                .AddTransient<IClock, Clock>()
                .AddTransient<ITlsClientFactory, TlsClientFactory>()
                .AddTransient<ITlsClient, SmtpTlsClient>()
                .AddTransient<ISmtpClient, SmtpClient>()
                .AddSingleton<IMxTesterConfig>(config)
                .AddSingleton<IBouncyCastleClientConfig>(config)
                .AddTransient<ISmtpSerializer, SmtpSerializer>()
                .AddTransient<ISmtpDeserializer, SmtpDeserializer>()
                .AddSerilogLogging()
                .BuildServiceProvider();

            return Provider
                .GetService<ITlsSecurityTester>();
        }

        class TlsClientFactory : ITlsClientFactory
        {
            public ITlsClient Create()
            {
                return Provider.GetService<ITlsClient>();
            }
        }
    }
}