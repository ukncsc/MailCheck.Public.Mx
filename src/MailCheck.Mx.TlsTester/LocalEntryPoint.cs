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

        public static void Main(string[] args)
        {
            CommandLineApplication commandLineApplication = new CommandLineApplication(false);
            commandLineApplication.Name = "MxTlsTester";
            
            commandLineApplication.HelpOption("-? | -h | --help");

            commandLineApplication.OnExecute(() =>
            {
                AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnProcessExit;

                _cancellationTokenSource = new CancellationTokenSource();

                IMxSecurityTesterProcessor mxSecurityTesterProcessor =
                    MxSecurityTesterFactory.CreateMxSecurityTesterProcessor();

                mxSecurityTesterProcessor.Process(_cancellationTokenSource.Token).Wait();

                return 0;
            });

            commandLineApplication.Execute(args);
        }

        private static void CurrentDomainOnProcessExit(object sender, EventArgs e)
        {
            _cancellationTokenSource.Cancel();
            System.Console.WriteLine("MxTlsTester is exiting...");
        }
    }
}