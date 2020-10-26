using System;
using System.Threading;
using MailCheck.Mx.TlsTester.Factory;
using MailCheck.Mx.TlsTester.MxTester;
using Microsoft.Extensions.CommandLineUtils;

namespace MailCheck.Mx.TlsTester
{
    public static class LocalEntryPoint
    {
        private static CancellationTokenSource _cancellationTokenSource;

        public static int Main(string[] args)
        {
            CommandLineApplication commandLineApplication = new CommandLineApplication(false);
            commandLineApplication.Name = "MxTlsTester";
            
            commandLineApplication.HelpOption("-? | -h | --help");

            commandLineApplication.OnExecute(async () =>
            {
                AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnProcessExit;
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

                _cancellationTokenSource = new CancellationTokenSource();

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
    }
}