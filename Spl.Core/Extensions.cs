using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Spl.Core
{
    [PublicAPI]
    public static class StringExtensions
    {
        // https://stackoverflow.com/a/9367156
        public static string ReplaceAt(this string str, int index, char newChar)
        {
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }

            var chars = str.ToCharArray();
            chars[index] = newChar;

            return new string(chars);
        }
    }

    [PublicAPI]
    public static class CollectionExtensions
    {
        // For a given array, pick a random entry.
        public static T? RandomElement<T>(this T[] coll)
        {
            return RandomElement(coll, SplRandom.Random);
        }

        // For a given array, pick a random entry.
        public static T? RandomElement<T>(this T[] coll, Random rnd)
        {
            return coll.Length == 0 ? default : coll[rnd.Next(0, coll.Length)];
        }

        // For a given list, pick a random entry.
        public static T? RandomElement<T>(this List<T> coll)
        {
            return RandomElement(coll, SplRandom.Random);
        }

        // For a given list, pick a random entry.
        public static T? RandomElement<T>(this List<T> coll, Random rnd)
        {
            return coll.Count == 0 ? default : coll[rnd.Next(0, coll.Count)];
        }

        // For a given enum, pick a random entry.
        public static T RandomEnumElement<T>(this T _)
            where T : struct, Enum
        {
            var values = Enum.GetValues<T>();
            return values[SplRandom.Random.Next(values.Length)];
        }
    }
}
