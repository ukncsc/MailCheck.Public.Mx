using System.Collections.Generic;
using System.Linq;

namespace MailCheck.Mx.TlsEntity.Entity.Notifiers
{
    public class Advisories<T> where T : class
    {
        public List<T> Added { get; set; }
        public List<T> Sustained { get; set; }
        public List<T> Removed { get; set; }

        public Advisories(IEnumerable<T> currentAdvisories, IEnumerable<T> newAdvisories)
        {
            currentAdvisories = currentAdvisories ?? Enumerable.Empty<T>();
            newAdvisories = newAdvisories ?? Enumerable.Empty<T>();

            Added = newAdvisories.Except(currentAdvisories).ToList();
            Sustained = currentAdvisories.Intersect(newAdvisories).ToList();
            Removed = currentAdvisories.Except(newAdvisories).ToList();
        }
    }
}