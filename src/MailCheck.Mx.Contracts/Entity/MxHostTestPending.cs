using System.Collections.Generic;
using MailCheck.Common.Messaging.Abstractions;

namespace MailCheck.Mx.Contracts.Entity
{
    public class MxHostTestPending : Message
    {
        public List<string> IpAddresses { get; private set; }

        public MxHostTestPending(string id, List<string> ipAddresses) : base(id)
        {
            IpAddresses = ipAddresses;
        }
    }
}
