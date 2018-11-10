using System.Collections.Generic;

namespace PhoneNumbers.Internal
{
    static class AddAllExtensions
    {
        internal static void AddAll<T>(this HashSet<T> original, HashSet<T> toAdd)
        {
            foreach (var thing in toAdd)
            {
                original.Add(thing);
            }
        }
    }
}
