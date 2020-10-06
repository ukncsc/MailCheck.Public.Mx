using System.Collections.Generic;
using MailCheck.Mx.Contracts.Poller;

namespace MailCheck.Mx.Contracts.Entity
{
    public class MxRecordsUpdated : Common.Messaging.Abstractions.Message
    {
        public MxRecordsUpdated(string id, List<HostMxRecord> records) : base(id)
        {
            Records = records;
        }

        public List<HostMxRecord> Records { get; }

        public MxState State => MxState.Evaluated;
    }
}