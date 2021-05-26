using System.Collections.Generic;
using Amazon.SimpleNotificationService;
using Amazon.SimpleSystemsManagement;
using MailCheck.Common.Data.Abstractions;
using MailCheck.Common.Data.Implementations;
using MailCheck.Common.Environment.Abstractions;
using MailCheck.Common.Environment.FeatureManagement;
using MailCheck.Common.Environment.Implementations;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Common.SSM;
using MailCheck.Common.Util;
using MailCheck.Mx.Contracts.Poller;
using MailCheck.Mx.Entity.Config;
using MailCheck.Mx.Entity.Dao;
using MailCheck.Mx.Entity.Entity;
using MailCheck.Mx.Entity.Entity.Notifiers;
using Microsoft.Extensions.DependencyInjection;

namespace MailCheck.Mx.Entity.StartUp
{
    internal class StartUp : IStartUp
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddTransient<IClock, Clock>()
                .AddTransient<IConnectionInfoAsync, MySqlEnvironmentParameterStoreConnectionInfoAsync>()
                .AddTransient<IEnvironment, EnvironmentWrapper>()
                .AddTransient<IEnvironmentVariables, EnvironmentVariables>()
                .AddSingleton<IAmazonSimpleSystemsManagement, CachingAmazonSimpleSystemsManagementClient>()
                .AddTransient<IAmazonSimpleNotificationService, AmazonSimpleNotificationServiceClient>() 
                .AddTransient<IMxEntityDao, MxEntityDao>()
                .AddTransient<IMxEntityConfig, MxEntityConfig>()
                .AddTransient<IChangeNotifiersComposite, ChangeNotifiersComposite>()
                .AddTransient<IChangeNotifier, RecordChangedNotifier>()
                .AddTransient<IEqualityComparer<HostMxRecord>, RecordEqualityComparer>()
                .AddConditionally(
                    "NewScheduler",
                    featureActiveRegistrations =>
                    {
                        featureActiveRegistrations.AddTransient<MxEntityNewScheduler>();
                    },
                    featureInactiveRegistrations =>
                    {
                        featureInactiveRegistrations.AddTransient<MxEntity>();
                    });
        }
    }
}