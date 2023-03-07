namespace edt.casemanagement.HttpClients.Services;

using DomainResults.Common;
using EdtService.Models;
using System.Linq.Expressions;
using System.Security.Claims;

public interface IEdtAuthorizationService
{

    /// <summary>
    /// Checks that the given Resource both exists and can be accessed by the given User, based on the given Policy.
    /// </summary>
    /// <typeparam name="T">The Type of the Resource</typeparam>
    /// <param name="predicate">Filtering predicate to find the Resource from the DB; usually an Id check eg: (Party p) => p.Id == partyId</param>
    /// <param name="user">The User to authorize against</param>
    /// <param name="policy">The Authorization policy to authorize against</param>
    Task<IDomainResult> CheckResourceAccessibility<T>(Expression<Func<T, bool>> predicate, ClaimsPrincipal user, string policy) where T : class, IOwnedResource;
}
