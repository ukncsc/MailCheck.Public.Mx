using System;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SimpleSystemsManagement;
using Amazon.SQS;
using MailCheck.Common.Environment.Abstractions;
using MailCheck.Common.Environment.Implementations;
using MailCheck.Common.Logging;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Common.Messaging.Sns;
using MailCheck.Common.SSM;
using MailCheck.Common.Util;
using MailCheck.Mx.TlsTester.Config;
using MailCheck.Mx.TlsTester.MxTester;
using MailCheck.Mx.TlsTester.Tls;
using MailCheck.Mx.TlsTester.Tls.Tests;
using Microsoft.Extensions.DependencyInjection;

namespace MailCheck.Mx.TlsTester.Factory
{
    public static class MxSecurityTesterFactory
    {
        public static IMxSecurityTesterProcessor CreateMxSecurityTesterProcessor()
        {
            IServiceProvider serviceProvider = new ServiceCollection()
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
                .AddTransient<IMxQueueProcessor, MxQueueProcessor>()
                .AddTransient<IAmazonSimpleNotificationService, AmazonSimpleNotificationServiceClient>()
                .AddTransient<IMxTesterConfig, MxTesterConfig>()
                .AddSingleton<IAmazonSimpleSystemsManagement, CachingAmazonSimpleSystemsManagementClient>()
                .AddTransient<IEnvironmentVariables, EnvironmentVariables>()
                .AddTransient<IEnvironment, EnvironmentWrapper>()
                .AddTransient<IMxSecurityTesterProcessor, MxSecurityTesterProcessor>()
                .AddTransient<ITlsSecurityTesterAdapator, TlsSecurityTesterAdapator>()
                .AddTransient<IMxSecurityProcessingFilter, MxSecurityProcessingFilter>()
                .AddTransient<IMessagePublisher, SnsMessagePublisher>()
                .AddTransient<IAmazonSQS>(_ => new AmazonSQSClient())
                .AddTransient<IClock, Clock>()
                .AddTransient<ITlsClientFactory, TlsClientFactory>()
                .AddTransient<IRecentlyProcessedLedger, RecentlyProcessedLedger>()
                .AddSerilogLogging()
                .BuildServiceProvider();

            return serviceProvider.GetService<IMxSecurityTesterProcessor>();
        }
    }
}