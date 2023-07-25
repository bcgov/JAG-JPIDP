namespace edt.disclosure.HttpClients.Services;

using DomainResults.Common;
using edt.disclosure.Infrastructure.Auth;
using EdtDisclosureService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Security.Claims;

public class EdtAuthorizationService : IEdtAuthorizationService
{

    private readonly IAuthorizationService authService;


    public EdtAuthorizationService(IAuthorizationService authService)
    {
        this.authService = authService;
    }



    public async Task<IDomainResult> CheckResourceAccessibility<T>(Expression<Func<T, bool>> predicate, ClaimsPrincipal user, string policy) where T : class, IOwnedResource
    {
        return null;
    }

    private class OwnedResourceStub : IOwnedResource
    {
        public Guid UserId { get; set; }
    }

}
