using System;
using System.Collections.Generic;
using MailCheck.Mx.Contracts.Poller;

namespace MailCheck.Mx.Contracts.Entity
{
    public class MxEntityState
    {
        public string Id { get; }
        public MxState MxState { get; set; }
        public List<HostMxRecord> HostMxRecords { get; set; }
        public DateTime? LastUpdated { get; set; }
        public SharedDomain.Message Error { get; set; }

        public MxEntityState(string id)
        {
            Id = id;
            MxState = MxState.Created;
        }
    }
}