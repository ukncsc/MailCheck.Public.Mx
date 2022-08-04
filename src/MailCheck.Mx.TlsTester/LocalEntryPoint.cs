using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MailCheck.Mx.BouncyCastle.Config;
using MailCheck.Mx.TlsTester.Config;
using MailCheck.Mx.TlsTester.Factory;
using MailCheck.Mx.TlsTester.MxTester;
using Microsoft.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MailCheck.Mx.TlsTester
{
    public static class LocalEntryPoint
    {
        private static CancellationTokenSource _cancellationTokenSource;
        private static JsonSerializerSettings SerializerSettingsForConsole = new JsonSerializerSettings
        {
            Converters =
                {
                    new StringEnumConverter()
                }
        };

        public static int Main(string[] args)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnProcessExit;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            CommandLineApplication commandLineApplication = new CommandLineApplication(false);
            commandLineApplication.Name = "MxTlsTester";

            commandLineApplication.Command("test",
                (command) =>
                {
                    CommandOption hostnames = command.Option("--hostnames", "Enter the hostnames to test.", CommandOptionType.MultipleValue);
                    CommandOption tests = command.Option("--tests", "Enter the test IDs to test.", CommandOptionType.MultipleValue);

                    command.OnExecute(async () =>
                    {
                        var config = new ConsoleConfig();
                        var tester = TlsSecurityTesterFactory.CreateTester(config);
                        var testIds = tests.Values.Select(int.Parse).ToArray();

                        var allResults = await Task.WhenAll(hostnames.Values.Select(async hostname =>
                        {
                            Console.WriteLine($"Running TLS test for host {hostname}");
                            var results = await tester.Test(hostname, testIds);
                            return new { hostname, results };
                        }));

                        foreach (var x in allResults)
                        {
                            Console.WriteLine($"Result for host {x.hostname} is:\n{JsonConvert.SerializeObject(x.results, SerializerSettingsForConsole)}");
                        }

                        return 0;
                    });
                }
            );

            commandLineApplication.HelpOption("-? | -h | --help");

            commandLineApplication.OnExecute(async () =>
            {
                IMxSecurityTesterProcessor mxSecurityTesterProcessor =
                        MxSecurityTesterFactory.CreateMxSecurityTesterProcessor();
                await mxSecurityTesterProcessor.Process(_cancellationTokenSource.Token);
                return 0;
            });

            return commandLineApplication.Execute(args);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.IsTerminating)
            {
                Console.Error.WriteLine("MxTlsTester is terminating due to an unhandled exception");
            }

            Console.Error.WriteLine("MxTlsTester threw an unhandled exception: {0}", FormatException(e.ExceptionObject));
        }

        private static void CurrentDomainOnProcessExit(object sender, EventArgs e)
        {
            _cancellationTokenSource.Cancel();
            Console.WriteLine("MxTlsTester is exiting...");
        }

        public static string FormatException(object exception)
        {
            switch (exception)
            {
                case AggregateException ae:
                    return ae.Flatten().ToString();
                default:
                    return exception.ToString();
            }
        }

        class ConsoleConfig : IMxTesterConfig, IBouncyCastleClientConfig
        {
            public TimeSpan TcpSendReceiveTimeout => TimeSpan.FromSeconds(5);
            public TimeSpan TcpConnectionTimeout => TimeSpan.FromSeconds(5);

            public string SnsTopicArn => throw new NotImplementedException();

            public string SqsQueueUrl => throw new NotImplementedException();

            public string SmtpHostName => "gateway1.dev.mailcheck.service.ncsc.gov.uk";

            public string[] TlsTesterIgnoredHosts => new string[]{};

            public int BufferSize => throw new NotImplementedException();

            public int PublishBatchSize => throw new NotImplementedException();

            public int PublishBatchFlushIntervalSeconds => throw new NotImplementedException();

            public int PrintStatsIntervalSeconds => throw new NotImplementedException();

            public int SlowResponseThresholdSeconds => throw new NotImplementedException();

            public int TlsTesterThreadCount => throw new NotImplementedException();

            public int TlsTesterHostRetestPeriodSeconds => throw new NotImplementedException();
        }
    }
}