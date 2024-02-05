namespace Common.Utils;
using System;
using System.Security.Claims;

public static class AuthUtils
{
    public static T GetLoggedInUserId<T>(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var loggedInUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (typeof(T) == typeof(Guid))
        {
            return (T)Convert.ChangeType(loggedInUserId, typeof(T));
        }
        else if (typeof(T) == typeof(string))
        {
            return (T)Convert.ChangeType(loggedInUserId, typeof(T));
        }
        else if (typeof(T) == typeof(int) || typeof(T) == typeof(long))
        {
            return loggedInUserId != null ? (T)Convert.ChangeType(loggedInUserId, typeof(T)) : (T)Convert.ChangeType(0, typeof(T));
        }
        else
        {
            throw new Exception("Invalid type provided");
        }
    }

    public static string GetLoggedInUserName(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        return principal.FindFirstValue(ClaimTypes.Name);
    }

    public static string GetLoggedInUserEmail(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        return principal.FindFirstValue(ClaimTypes.Email);
    }
}
