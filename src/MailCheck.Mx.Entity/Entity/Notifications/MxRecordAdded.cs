using System.Collections.Generic;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Mx.Contracts.Poller;

namespace MailCheck.Mx.Entity.Entity.Notifications
{
    public class MxRecordAdded : Message
    {
        public MxRecordAdded(string id, List<HostMxRecord> records) : base(id)
        {
            Records = records;
        }

        public List<HostMxRecord> Records { get; }
    }
}