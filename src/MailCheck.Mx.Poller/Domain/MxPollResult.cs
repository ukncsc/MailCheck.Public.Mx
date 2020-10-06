using System;
using System.Collections.Generic;
using MailCheck.Mx.Contracts.Poller;

namespace MailCheck.Mx.Poller.Domain
{
    public class MxPollResult
    {
        public MxPollResult(string id, Error error)
            : this(id, null, null, error)
        {
        }

        public MxPollResult(string id, List<HostMxRecord> records, TimeSpan elapsed)
            : this(id, records, elapsed, null)
        {
        }

        private MxPollResult(string id, List<HostMxRecord> records, TimeSpan? elapsed, Error error)
        {
            Id = id;
            Records = records ?? new List<HostMxRecord>();
            Elapsed = elapsed;
            Error = error;
        }

        public List<HostMxRecord> Records { get; }
        public string Id { get; }
        public TimeSpan? Elapsed { get; }
        public Error Error { get; }
    }
}
