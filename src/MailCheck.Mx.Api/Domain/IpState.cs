using System;
using System.Collections.Generic;

namespace MailCheck.Mx.Api.Domain
{
    public class IpState
    {
        public IpState(string ipAddress, DateTime? tlsLastUpdated, DateTime? certsLastUpdated)
        {
            IpAddress = ipAddress;
            TlsLastUpdated = tlsLastUpdated;
            CertsLastUpdated = certsLastUpdated;
        }

        public string IpAddress { get; }
        public DateTime? TlsLastUpdated { get; }
        public DateTime? CertsLastUpdated { get; }
    }

    public class IpStateComparer : IEqualityComparer<IpState>
    {
        public bool Equals(IpState x, IpState y)
        {
            return x?.IpAddress == y?.IpAddress;
        }

        public int GetHashCode(IpState obj)
        {
            return obj.IpAddress != null
                ? obj.IpAddress.GetHashCode()
                : 0;
        }
    }
}
