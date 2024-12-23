namespace Common.Helpers.Extensions;

using System;

public static class StringExtensions
{

    public static string EnsureTrailingSlash(this string url)
    {
        if (!url.EndsWith("/", StringComparison.Ordinal))
        {
            return url + "/";
        }

        return url;
    }
}

