namespace edt.casemanagement.Features;

using DomainResults.Common;
using edt.casemanagement.HttpClients.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

using edt.casemanagement.Telemetry;
using MediatR;

[Produces("application/json")]
[ApiController]
public class EdtControllerBase : ControllerBase
{
  //  protected IEdtAuthorizationService AuthorizationService { get; }
    protected readonly IMediator _mediator;

   // protected EdtControllerBase(IEdtAuthorizationService authService) => this.AuthorizationService = authService;

    protected EdtControllerBase(IMediator mediator)
    {
        this._mediator = mediator;
    }

}
