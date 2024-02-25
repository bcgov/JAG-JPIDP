namespace Common.Utils;
using System;
using System.Linq;
using Microsoft.AspNetCore.Http;

public class HttpUtils
{

    /// <summary>
    /// From request headers try to determine the incoming host
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public static string GetHostFromHeader(HttpRequest request, bool preferReferer)
    {
        var headers = request.Headers;
        var hasHost = headers.TryGetValue(Microsoft.Net.Http.Headers.HeaderNames.Host, out var hostName);
        var hasReferer = headers.TryGetValue(Microsoft.Net.Http.Headers.HeaderNames.Referer, out var referer);

        if (hasHost || hasReferer)
        {
            var hostname = hostName.FirstOrDefault();
            Serilog.Log.Information($"Getting host Host={hostName} Referer={referer}");
            if (preferReferer)
            {
                hostname = referer.FirstOrDefault();
                hostname = new Uri(referer).Host;
            }
            else if (hasHost)
            {
                hostname = hostname.Contains(":") ? hostname.Split(":")[0] : hostname;
            }
            else
            {
                hostname = referer.FirstOrDefault();
                hostname = new Uri(referer).Host;
            }
            return hostname;

        }

        return "";
    }
}
