using System.Collections.Generic;
using System.Linq;

namespace MailCheck.Mx.TlsEntity
{
    internal static class CollectionExtensionMethods
    {
        public static bool CollectionEqual<T>(this IEnumerable<T> x, IEnumerable<T> y, IEqualityComparer<T> equalityComparer = null)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.SequenceEqual(y, equalityComparer ?? EqualityComparer<T>.Default);
        }
    }
}
