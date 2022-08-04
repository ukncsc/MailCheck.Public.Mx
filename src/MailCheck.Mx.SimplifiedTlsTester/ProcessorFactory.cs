using System;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using MailCheck.Common.Environment.Abstractions;
using MailCheck.Common.Environment.Implementations;
using MailCheck.Common.Logging;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Common.Messaging.Sns;
using MailCheck.Mx.SimplifiedTlsTester.TestRunner;
using Microsoft.Extensions.DependencyInjection;

namespace MailCheck.Mx.SimplifiedTlsTester
{
    public static class ProcessorFactory
    {
        public static IProcessor CreateProcessor()
        {
            IServiceProvider serviceProvider = new ServiceCollection()
                .AddTransient<IEnvironmentVariables, EnvironmentVariables>()
                .AddTransient<IEnvironment, EnvironmentWrapper>()
                .AddTransient<ISqsClient, SqsClient>()
                .AddTransient<IProcessor, Processor>()
                .AddTransient<IProcessorConfig, HostedConfig>()
                .AddTestRunner<HostedConfig>()
                .AddTransient<IMessagePublisher, SnsMessagePublisher>()
                .AddTransient<IAmazonSimpleNotificationService, AmazonSimpleNotificationServiceClient>()
                .AddTransient<IAmazonSQS, AmazonSQSClient>()
                .AddSingleton<IContainerConfig, ContainerConfig>()
                .AddSerilogLogging()
                .BuildServiceProvider();

            return serviceProvider.GetService<IProcessor>();
        }
    }
}