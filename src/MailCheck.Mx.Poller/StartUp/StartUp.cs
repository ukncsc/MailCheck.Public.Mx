using System;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using Amazon.SimpleNotificationService;
using DnsClient;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Mx.Contracts.Entity;
using MailCheck.Mx.Poller.Config;
using MailCheck.Mx.Poller.Dns;
using Microsoft.Extensions.DependencyInjection;
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
                .AddTransient<IDnsNameServerProvider, LinuxDnsNameServerProvider>()
                .AddTransient<IAmazonSimpleNotificationService, AmazonSimpleNotificationServiceClient>()
                .AddTransient<IMxProcessor, MxProcessor>()
                .AddTransient<IHandle<MxPollPending>, PollHandler>()
                .AddTransient<IMxPollerConfig, MxPollerConfig>();
        }

        private static ILookupClient CreateLookupClient(IServiceProvider provider)
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? new LookupClient(NameServer.GooglePublicDns, NameServer.GooglePublicDnsIPv6)
                {
                    Timeout = provider.GetRequiredService<IMxPollerConfig>().DnsRecordLookupTimeout
                }
                : new LookupClient(provider.GetService<IDnsNameServerProvider>().GetNameServers()
                    .Select(_ => new IPEndPoint(_, 53)).ToArray())
                {
                    Timeout = provider.GetRequiredService<IMxPollerConfig>().DnsRecordLookupTimeout,
                    UseTcpOnly = true,
                };
        }
    }
}
