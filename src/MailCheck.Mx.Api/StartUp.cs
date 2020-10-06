using System;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleSystemsManagement;
using FluentValidation;
using FluentValidation.AspNetCore;
using MailCheck.Common.Api.Authentication;
using MailCheck.Common.Api.Authorisation.Service;
using MailCheck.Common.Api.Middleware;
using MailCheck.Common.Api.Middleware.Audit;
using MailCheck.Common.Data.Abstractions;
using MailCheck.Common.Data.Implementations;
using MailCheck.Common.Logging.Telemetry;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Common.Messaging.Sns;
using MailCheck.Common.SSM;
using MailCheck.Common.Util;
using MailCheck.Mx.Api.Config;
using MailCheck.Mx.Api.Dao;
using MailCheck.Mx.Api.Domain;
using MailCheck.Mx.Api.Service;
using MailCheck.Mx.Api.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.HealthChecks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MailCheck.Mx.Api
{
    public class StartUp
    {
        public StartUp(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            if (RunInDevMode())
            {
                services.AddCors(CorsOptions);
            }

            ServiceCollectionExtensions.AddLogging(services
                    .AddHealthChecks(checks =>
                        checks.AddValueTaskCheck("HTTP Endpoint", () =>
                            new ValueTask<IHealthCheckResult>(HealthCheckResult.Healthy("Ok"))))
                    .AddTransient<IConnectionInfoAsync, MySqlEnvironmentParameterStoreConnectionInfoAsync>()
                    .AddSingleton<IAmazonSimpleSystemsManagement, CachingAmazonSimpleSystemsManagementClient>()
                    .AddTransient<IDomainValidator, DomainValidator>()
                    .AddTransient<IMessagePublisher, SnsMessagePublisher>()
                    .AddTransient<IAmazonSimpleNotificationService, AmazonSimpleNotificationServiceClient>()
                    .AddTransient<IValidator<DomainRequest>, DomainRequestValidator>()
                    .AddTransient<IMxService, MxService>()
                    .AddTransient<IMxApiDao, MxApiDao>()
                    .AddTransient<IDomainTlsEvaluatorResultsFactory, DomainTlsEvaluatorResultsFactory>()
                    .AddTransient<IMxApiConfig, MxApiConfig>()
                    .AddAudit("MX-Api")
                    .AddMailCheckAuthenticationClaimsPrincipleClient())
                    .AddMvc(config =>
                {
                    AuthorizationPolicy policy = new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .Build();
                    config.Filters.Add(new AuthorizeFilter(policy));
                })
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;
                    options.SerializerSettings.Converters.Add(new StringEnumConverter());
                })
                .AddFluentValidation();

            services
                .AddAuthorization()
                .AddAuthentication(AuthenticationSchemes.Claims)
                .AddMailCheckClaimsAuthentication();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            new TelemetryConfig()
                .InstrumentAwsSdk()
                .InstrumentFlurlHttp()
                .InstrumentAspNet(app, "MailCheck.Mx.Api");

            if (RunInDevMode())
            {
                app.UseCors(CorsPolicyName);
            }

            app.UseMiddleware<AuditTimerMiddleware>()
               .UseMiddleware<OidcHeadersToClaimsMiddleware>()
               .UseMiddleware<ApiKeyToClaimsMiddleware>()
               .UseAuthentication()
               .UseMiddleware<AuditLoggingMiddleware>()
               .UseMiddleware<UnhandledExceptionMiddleware>()
               .UseMvc();
        }

        private bool RunInDevMode()
        {
            bool.TryParse(Environment.GetEnvironmentVariable("DevMode"), out bool isDevMode);
            return isDevMode;
        }

        private static Action<CorsOptions> CorsOptions => options =>
        {
            options.AddPolicy(CorsPolicyName, builder =>
                builder
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
        };

        private const string CorsPolicyName = "CorsPolicy";
    }
}