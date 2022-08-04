using System;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using Amazon.SimpleNotificationService;
using DnsClient;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Common.Util;
using MailCheck.Mx.Contracts.Entity;
using MailCheck.Mx.Poller.Config;
using MailCheck.Mx.Poller.Dns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace MailCheck.Mx.Poller.StartUp
{
    internal class StartUp : IStartUp
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
                .AddTransient<MxProcessor>()
                .AddSingleton(CreateLookupClient)
                .AddTransient<IDnsClient, Dns.DnsClient>()
                .AddTransient<IAuditTrailParser, AuditTrailParser>()
                .AddTransient<IDnsNameServerProvider, LinuxDnsNameServerProvider>()
                .AddTransient<IAmazonSimpleNotificationService, AmazonSimpleNotificationServiceClient>()
                .AddTransient<IMxProcessor, MxProcessor>()
                .AddTransient<IHandle<MxPollPending>, PollHandler>()
                .AddTransient<IMxPollerConfig, MxPollerConfig>();
        }

        private static ILookupClient CreateLookupClient(IServiceProvider provider)
        {
            LookupClient lookupClient =  RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? new LookupClient(NameServer.GooglePublicDns, NameServer.GooglePublicDnsIPv6)
                {
                    Timeout = provider.GetRequiredService<IMxPollerConfig>().DnsRecordLookupTimeout
                }
                : new LookupClient(new LookupClientOptions(
                    provider.GetService<IDnsNameServerProvider>().GetNameServers()
                    .Select(_ => new IPEndPoint(_, 53)).ToArray())
                {
                    ContinueOnEmptyResponse = false,
                    UseCache = false,
                    UseTcpOnly = true,
                    EnableAuditTrail = true,
                    Retries = 0,
                    Timeout = provider.GetRequiredService<IMxPollerConfig>().DnsRecordLookupTimeout,
                });

            return new AuditTrailLoggingLookupClientWrapper(lookupClient, provider.GetService<IAuditTrailParser>(), provider.GetService<ILogger<AuditTrailLoggingLookupClientWrapper>>());
        }
    }
}
