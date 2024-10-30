using System.Security.Claims;
using jumwebapi.Core.Extension;
using jumwebapi.Data.Exceptions;
using jumwebapi.Data.Security;
using jumwebapi.Infrastructure;

namespace jumwebapi.Extensions;

/// <summary>
/// IdentityExtensions static class, provides extension methods for user identity.
/// </summary>
public static class IdentityExtensions
{
    /// <summary>
    /// Determine if the user any of the specified permission.
    /// </summary>
    /// <param name="user"></param>
    /// <param name="permission"></param>
    /// <returns>True if the user has any of the permission.</returns>
    public static bool HasPermission(this ClaimsPrincipal user, params Permissions[] permission)
    {
        ArgumentNullException.ThrowIfNull(permission);
        if (permission.Length == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(permission));
        }

        var roles = permission.Select(r => r.GetName()).ToArray();
        return user.Claims.Any(c => c.Type == ClaimTypes.Role && roles.Contains(c.Value));
    }

    /// <summary>
    /// Determine if the user all of the specified permissions.
    /// </summary>
    /// <param name="user"></param>
    /// <param name="permission"></param>
    /// <returns>True if the user has all of the permissions.</returns>
    public static bool HasPermissions(this ClaimsPrincipal user, params Permissions[] permission)
    {
        ArgumentNullException.ThrowIfNull(permission);
        if (permission.Length == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(permission));
        }

        var roles = permission.Select(r => r.GetName()).ToArray();
        var claims = user.Claims.Where(c => c.Type == ClaimTypes.Role);
        return roles.All(r => claims.Any(c => c.Value == r));
    }

    /// <summary>
    /// If the user does has not been authenticated throw a NotAuthorizedException.
    /// </summary>
    /// <param name="user"></param>
    /// <param name="message"></param>
    /// <exception type="NotAuthorizedException">User does not have the specified 'role'.</exception>
    /// <returns></returns>
    public static ClaimsPrincipal ThrowIfNotAuthorized(this ClaimsPrincipal user, string message = null)
    {
        if (user == null || !user.Identity.IsAuthenticated)
        {
            throw new NotAuthorizedException(message);
        }

        return user;
    }

    /// <summary>
    /// If the user does not have the specified 'role' throw a NotAuthorizedException.
    /// </summary>
    /// <param name="user"></param>
    /// <param name="role"></param>
    /// <param name="message"></param>a
    /// <exception type="NotAuthorizedException">User does not have the specified 'role'.</exception>
    /// <returns></returns>
    public static ClaimsPrincipal ThrowIfNotAuthorized(this ClaimsPrincipal user, string role, string message)
    {
        if (user == null || !user.HasRole(role))
        {
            throw new NotAuthorizedException(message);
        }

        return user;
    }

    /// <summary>
    /// If the user does not have the specified 'permission' throw a NotAuthorizedException.
    /// </summary>
    /// <param name="user"></param>
    /// <param name="permission"></param>
    /// <param name="message"></param>
    /// <exception type="NotAuthorizedException">User does not have the specified 'permission'.</exception>
    /// <returns></returns>
    public static ClaimsPrincipal ThrowIfNotAuthorized(this ClaimsPrincipal user, Permissions permission, string message = null)
    {
        if (user == null || !user.HasPermission(permission))
        {
            throw new NotAuthorizedException(message);
        }

        return user;
    }

    /// <summary>
    /// If the user does not have any of the specified 'permission' throw a NotAuthorizedException.
    /// </summary>
    /// <param name="user"></param>
    /// <param name="permission"></param>
    /// <exception type="NotAuthorizedException">User does not have the specified 'role'.</exception>
    /// <returns></returns>
    public static ClaimsPrincipal ThrowIfNotAuthorized(this ClaimsPrincipal user, params Permissions[] permission)
    {
        if (user == null || !user.HasPermission(permission))
        {
            throw new NotAuthorizedException();
        }

        return user;
    }

    /// <summary>
    /// If the user does not have the all specified 'permission' throw a NotAuthorizedException.
    /// </summary>
    /// <param name="user"></param>
    /// <param name="permission"></param>
    /// <param name="message"></param>
    /// <exception type="NotAuthorizedException">User does not have the specified 'permission'.</exception>
    /// <returns></returns>
    public static ClaimsPrincipal ThrowIfNotAllAuthorized(this ClaimsPrincipal user, Permissions permission, string message = null)
    {
        if (user == null || !user.HasPermissions(permission))
        {
            throw new NotAuthorizedException(message);
        }

        return user;
    }

    /// <summary>
    /// If the user does not have all of the specified 'permission' throw a NotAuthorizedException.
    /// </summary>
    /// <param name="user"></param>
    /// <param name="permission"></param>
    /// <exception type="NotAuthorizedException">User does not have the specified 'role'.</exception>
    /// <returns></returns>
    public static ClaimsPrincipal ThrowIfNotAllAuthorized(this ClaimsPrincipal user, params Permissions[] permission)
    {
        if (user == null || !user.HasPermissions(permission))
        {
            throw new NotAuthorizedException();
        }

        return user;
    }

    /// <summary>
    /// If the user does not have any of the specified 'permission' throw a NotAuthorizedException.
    /// </summary>
    /// <param name="user"></param>
    /// <param name="permission"></param>
    /// <param name="message"></param>
    /// <exception type="NotAuthorizedException">User does not have the specified 'role'.</exception>
    /// <returns></returns>
    public static ClaimsPrincipal ThrowIfNotAuthorized(this ClaimsPrincipal user, Permissions[] permission, string message = null)
    {
        if (user == null || !user.HasPermission(permission))
        {
            throw new NotAuthorizedException(message);
        }

        return user;
    }

    /// <summary>
    /// Throw exception if the 'user' is not allowed to edit the specified entity.
    /// </summary>
    /// <param name="user"></param>
    /// <param name="paramName"></param>
    /// <param name="entity"></param>
    /// <param name="permission"></param>
    /// <param name="message"></param>
    /// <typeparam name="T"></typeparam>
    /// <exception type="ArgumentNullException">Entity argument cannot be null.</exception>
    /// <exception type="ConcurrencyControlNumberMissingException">Entity.ConcurrencyControlNumber cannot be null.</exception>
    /// <exception type="NotAuthorizedException">User must have specified 'role'.</exception>
    /// <returns></returns>
    public static T ThrowIfNotAllowedToEdit<T>(this ClaimsPrincipal user, string paramName, T entity, Permissions permission, string message = null) where T : class, IBaseEntity
    {
        entity.ThrowIfNull(paramName);
        user.ThrowIfNotAuthorized(permission, message);

        return entity;
    }

    /// <summary>
    /// Throw exception if the 'user' is not allowed to edit the specified entity.
    /// </summary>
    /// <param name="user"></param>
    /// <param name="paramName"></param>
    /// <param name="entity"></param>
    /// <param name="permission"></param>
    /// <param name="message"></param>
    /// <typeparam name="T"></typeparam>
    /// <exception type="ArgumentNullException">Entity argument cannot be null.</exception>
    /// <exception type="ConcurrencyControlNumberMissingException">Entity.ConcurrencyControlNumber cannot be null.</exception>
    /// <exception type="NotAuthorizedException">User must have specified 'role'.</exception>
    /// <returns></returns>
    public static T ThrowIfNotAllowedToEdit<T>(this ClaimsPrincipal user, string paramName, T entity, Permissions[] permission, string message = null) where T : class, IBaseEntity
    {
        entity.ThrowIfNull(paramName);
        user.ThrowIfNotAuthorized(permission, message);

        return entity;
    }
}
