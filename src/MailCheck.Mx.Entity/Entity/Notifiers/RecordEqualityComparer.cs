using System;
using System.Collections.Generic;
using MailCheck.Mx.Contracts.Poller;

namespace MailCheck.Mx.Entity.Entity.Notifiers
{
    public class RecordEqualityComparer : IEqualityComparer<HostMxRecord>
    {
        public bool Equals(HostMxRecord x, HostMxRecord y)
        {
            return y != null && x != null && y.Preference == x.Preference && String.Equals(y.Id, x.Id, StringComparison.InvariantCultureIgnoreCase);
        }

        public int GetHashCode(HostMxRecord obj)
        {
            return obj.Id.ToLower().GetHashCode();
        }
    }
}