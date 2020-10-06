using System.Collections.Generic;
using System.Linq;
using MailCheck.Common.Messaging.Abstractions;
using Newtonsoft.Json;

namespace MailCheck.Mx.Contracts.Poller
{
    public class HostMxRecord : Message
    {
        public int? Preference { get; }
        public List<string> IpAddresses { get; }

        [JsonConstructor]
        public HostMxRecord(string id, int? preference, List<string> ipAddresses) : base(id)
        {
            Preference = preference;
            IpAddresses = ipAddresses;
        }

        protected bool Equals(HostMxRecord other)
        {
            bool hostNameEqual = Id?.Replace("; ", ";") == other.Id?.Replace("; ", ";");
            bool preferenceEqual = Preference == other.Preference;
            bool ipAddressesEqual = (IpAddresses == null && other.IpAddresses==null) || 
                                    (IpAddresses != null && other.IpAddresses != null) && IpAddresses.OrderBy(i => i).SequenceEqual(other.IpAddresses.OrderBy(i => i));

            return hostNameEqual && preferenceEqual && ipAddressesEqual;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((HostMxRecord) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (Id != null ? Id.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Preference.GetHashCode();
                hashCode = (hashCode * 397) ^ (IpAddresses != null ? IpAddresses.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}