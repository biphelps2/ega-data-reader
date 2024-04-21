using System.Collections.Generic;
using System.Linq;

namespace Spl.Core.Gif
{
    public static class ListHash
    {
        // https://stackoverflow.com/a/30758270/11678918
        public static int GetSequenceHashCode<T>(IEnumerable<T> sequence)
        {
            const int seed = 487;
            const int modifier = 31;

            unchecked
            {
                return sequence.Aggregate(seed, (current, item) => current * modifier + item!.GetHashCode());
            }
        }

        // We can "add" a number to a hashed list.
        public static int GetSequenceHashCodeAddItem<T>(int hash, T nextItem)
        {
            const int modifier = 31;

            unchecked
            {
                return hash * modifier + nextItem!.GetHashCode();
            }
        }
    }
}
