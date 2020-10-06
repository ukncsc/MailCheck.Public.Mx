using MailCheck.Common.Messaging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Amazon.SimpleNotificationService;
using Amazon.SimpleSystemsManagement;
using MailCheck.Common.Data.Abstractions;
using MailCheck.Common.Data.Implementations;
using MailCheck.Common.Environment.Abstractions;
using MailCheck.Common.Environment.Implementations;
using MailCheck.Common.SSM;
using MailCheck.Common.Util;
using MailCheck.Mx.TlsEntity.Config;
using MailCheck.Mx.TlsEntity.Dao;
using MailCheck.Mx.TlsEntity.Entity.DomainStatus;
using MailCheck.Mx.TlsEntity.Entity.Notifiers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace MailCheck.Mx.TlsEntity.StartUp
{
    public class StartUp : IStartUp
    {
        public void ConfigureServices(IServiceCollection services)
        {
            JsonConvert.DefaultSettings = () =>
            {
                JsonSerializerSettings serializerSetting = new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    ReferenceLoopHandling = ReferenceLoopHandling.Serialize
                };

                serializerSetting.Converters.Add(new StringEnumConverter());

                return serializerSetting;
            };

            services
                .AddTransient<IClock, Clock>()
                .AddTransient<IChangeNotifier,AdvisoryChangedNotifier>()
                .AddTransient<IChangeNotifiersComposite, ChangeNotifiersComposite>()
                .AddTransient<IConnectionInfoAsync, MySqlEnvironmentParameterStoreConnectionInfoAsync>()
                .AddTransient<IEnvironment, EnvironmentWrapper>()
                .AddTransient<IEnvironmentVariables, EnvironmentVariables>()
                .AddSingleton<IAmazonSimpleSystemsManagement, CachingAmazonSimpleSystemsManagementClient>()
                .AddTransient<IAmazonSimpleNotificationService, AmazonSimpleNotificationServiceClient>()
                .AddTransient<ITlsEntityDao, TlsEntityDao>()
                .AddTransient<ITlsEntityConfig, TlsEntityConfig>()
                .AddTransient<IDomainStatusPublisher, DomainStatusPublisher>()
                .AddTransient<IDomainStatusEvaluator, DomainStatusEvaluator>()
                .AddTransient<Entity.TlsEntity>();
        }
    }
}
