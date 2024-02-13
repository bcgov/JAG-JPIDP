namespace Pidp.Features;

using System.Diagnostics;
using DomainResults.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Pidp.Infrastructure.Services;
using Pidp.Infrastructure.Telemetry;

[Produces("application/json")]
[ApiController]
[Authorize]
public class PidpControllerBase : ControllerBase
{
    protected IPidpAuthorizationService AuthorizationService { get; }

    protected PidpControllerBase(IPidpAuthorizationService authService) => this.AuthorizationService = authService;

    /// <summary>
    /// Checks that the given Party both exists and is owned by the current User before executing the handler.
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <param name="partyId"></param>
    /// <param name="handler"></param>
    /// <param name="request"></param>
    protected async Task<IDomainResult> AuthorizePartyBeforeHandleAsync<TRequest>(int partyId, IRequestHandler<TRequest, IDomainResult> handler, TRequest request)
    {

        using (var activity = Telemetry.ActivitySource.StartActivity("AuthorizeParty"))
        {
            Activity.Current?.AddTag("digitalevidence.party.id", partyId);

            var access = await this.AuthorizationService.CheckPartyAccessibility(partyId, this.User);
            if (access.IsSuccess)
            {
                return await handler.HandleAsync(request);
            }


            return access;
        }
    }

    /// <summary>
    /// Checks that the given Party both exists and is owned by the current User before executing the handler.
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="partyId"></param>
    /// <param name="handler"></param>
    /// <param name="request"></param>
    protected async Task<IDomainResult<TResult>> AuthorizePartyBeforeHandleAsync<TRequest, TResult>(int partyId, IRequestHandler<TRequest, IDomainResult<TResult>> handler, TRequest request)
    {
        using (var activity = Telemetry.ActivitySource.StartActivity("CheckPartyAccessibility"))
        {
            Activity.Current?.AddTag("digitalevidence.party.id", partyId);

            var access = await this.AuthorizationService.CheckPartyAccessibility(partyId, this.User);
            if (access.IsSuccess)
            {
                return await handler.HandleAsync(request);
            }

            return access.To<TResult>();
        }
    }

    /// <summary>
    /// Checks that the given Party both exists and is owned by the current User before executing the handler.
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <param name="partyId"></param>
    /// <param name="handler"></param>
    /// <param name="request"></param>
    protected async Task<IDomainResult> AuthorizePartyBeforeHandleAsync<TRequest>(int partyId, IRequestHandler<TRequest> handler, TRequest request)
    {

        using (var activity = Telemetry.ActivitySource.StartActivity("CheckPartyAccessibility"))
        {
            Activity.Current?.AddTag("digitalevidence.party.id", partyId);

            var access = await this.AuthorizationService.CheckPartyAccessibility(partyId, this.User);
            if (access.IsSuccess)
            {
                await handler.HandleAsync(request);
                return DomainResult.Success();
            }

            return access;
        }
    }

    /// <summary>
    /// Checks that the given Party both exists and is owned by the current User before executing the handler.
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="partyId"></param>
    /// <param name="handler"></param>
    /// <param name="request"></param>
    protected async Task<IDomainResult<TResult>> AuthorizePartyBeforeHandleAsync<TRequest, TResult>(int partyId, IRequestHandler<TRequest, TResult> handler, TRequest request)
    {
        using (var activity = Telemetry.ActivitySource.StartActivity("CheckPartyAccessibility"))
        {
            Activity.Current?.AddTag("digitalevidence.party.id", partyId);

            var access = await this.AuthorizationService.CheckPartyAccessibility(partyId, this.User);
            if (access.IsSuccess)
            {
                return DomainResult.Success(await handler.HandleAsync(request));
            }
            else
            {
                Serilog.Log.Warning($"Party access failure {access.Status}");
            }

            return access.To<TResult>();
        }
    }

}
