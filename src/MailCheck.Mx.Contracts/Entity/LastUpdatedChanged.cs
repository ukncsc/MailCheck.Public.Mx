using System;
using MailCheck.Mx.Contracts.Entity;

namespace MailCheck.Mx.Contracts.Entity
{
    public class LastUpdatedChanged : Common.Messaging.Abstractions.Message
    {
        public LastUpdatedChanged(string id, DateTime lastUpdated) 
            : base(id)
        {
            LastUpdated = lastUpdated;
        }

        public DateTime LastUpdated { get; }

        public MxState State => MxState.Unchanged;
    }
}