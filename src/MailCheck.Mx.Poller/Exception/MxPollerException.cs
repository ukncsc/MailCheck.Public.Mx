using System;
using System.Collections.Generic;
using System.Text;

namespace MailCheck.Mx.Poller.Exception
{
    public class MxPollerException : System.Exception
    {
        public MxPollerException()
        {
        }

        public MxPollerException(string formatString, params object[] values)
            : base(string.Format(formatString, values))
        {
        }

        public MxPollerException(string message)
            : base(message)
        {
        }
    }
}
