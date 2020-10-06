using System;
using System.Collections.Generic;
using MailCheck.Common.Messaging.Abstractions;

namespace MailCheck.Mx.Contracts.Poller
{
    public class MxRecordsPolled : Message
    {
        public SharedDomain.Message Error { get; }
        public List<HostMxRecord> Records { get; }
        public TimeSpan? ElapsedQueryTime { get; }

        public MxRecordsPolled(string id, List<HostMxRecord> records, TimeSpan? elapsedQueryTime, SharedDomain.Message error = null) : base(id)
        {
            Records = records;
            ElapsedQueryTime = elapsedQueryTime;
            Error = error;
        }
    }
}
