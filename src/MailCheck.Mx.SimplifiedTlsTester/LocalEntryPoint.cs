using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MailCheck.Common.Logging;
using MailCheck.Mx.SimplifiedTlsTester.TestRunner;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MailCheck.Mx.SimplifiedTlsTester
{
    public static class LocalEntryPoint
    {
        private static readonly JsonSerializerSettings SerializerSettingsForConsole = new JsonSerializerSettings
        {
            Converters =
                {
                    new StringEnumConverter()
                }
        };
        private static CancellationTokenSource _cancellationTokenSource;

        public static int Main(string[] args)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnProcessExit;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            CommandLineApplication commandLineApplication = new CommandLineApplication { Name = "SimplifiedTlsTester" };

            commandLineApplication.Command("test",
                (command) =>
                {
                    CommandOption ips = command.Option("--ips", "Enter the IPs to test.", CommandOptionType.MultipleValue);

                    command.OnExecute(async () =>
                    {
                        var tester = new ServiceCollection()
                            .AddTestRunner<ConsoleConfig>()
                            .AddSerilogLogging()
                            .BuildServiceProvider()
                            .GetService<ITestRunner>();

                        var allResults = await Task.WhenAll(ips.Values.Select(async ip =>
                        {
                            Console.WriteLine($"Running TLS test for IP {ip}");
                            var results = await tester.Run(ip);
                            return new { ip, results };
                        }));

                        foreach (var x in allResults)
                        {
                            Console.WriteLine($"Result for IP {x.ip} is:\n{JsonConvert.SerializeObject(x.results, SerializerSettingsForConsole)}");
                        }

                        return 0;
                    });
                }
            );

            commandLineApplication.OnExecute(async () =>
            {
                Console.WriteLine("SimplifiedTlsTester is starting...");
                IProcessor processor = ProcessorFactory.CreateProcessor();
                await processor.Process(_cancellationTokenSource.Token);
                return 0;
            });

            return commandLineApplication.Execute(args);
        }

        private static void CurrentDomainOnProcessExit(object sender, EventArgs e)
        {
            _cancellationTokenSource.Cancel();
            Console.WriteLine("SimplifiedTlsTester is exiting...");
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.IsTerminating)
            {
                Console.Error.WriteLine("SimplifiedTlsTester is terminating due to an unhandled exception");
            }

            Console.Error.WriteLine("SimplifiedTlsTester threw an unhandled exception: {0}", FormatException(e.ExceptionObject));
        }

        public static string FormatException(object exception)
        {
            return exception switch
            {
                AggregateException ae => ae.Flatten().ToString(),
                _ => exception.ToString(),
            };
        }
    }
}