using System;

namespace AutoRetainerSort
{
    public static class Extensions
    {
        public static bool MatchesSearch(this string str, string[] searchWords)
        {
            for (int i = 0; i < searchWords.Length; i++)
            {
                if (str.IndexOf(searchWords[i], StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return false;
                }
            }

            return true;
        }
    }
}