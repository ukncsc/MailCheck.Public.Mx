using System.Collections.Generic;

namespace MailCheck.Mx.TlsTester
{
    public class CommandLineArgs
    {
        public CommandLineArgs(List<string> debug)
        {
            Debug = debug;
        }

        public List<string> Debug { get; }
    }
}