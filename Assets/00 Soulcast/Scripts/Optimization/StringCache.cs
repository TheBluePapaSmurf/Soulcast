// Create this script: Assets/Scripts/Optimization/StringCache.cs
using System.Collections.Generic;

public static class StringCache
{
    private static Dictionary<string, string> cache = new Dictionary<string, string>();

    public static string GetCachedString(string format, params object[] args)
    {
        string key = format + string.Join("", args);

        if (!cache.ContainsKey(key))
        {
            cache[key] = string.Format(format, args);
        }

        return cache[key];
    }

    public static void ClearCache()
    {
        cache.Clear();
    }
}
