using System.Collections.Generic;

namespace LogReader
{
    public static class DictionaryExtensionMethods
    {
        public static void TryIncrementOrAdd<T>(this Dictionary<T, int> dictionary, T key)
        {
            if (dictionary.TryGetValue(key, out int count))
            {
                dictionary[key] = count + 1;
            }
            else
            {
                dictionary[key] = 1;
            }
        }
    }
}
