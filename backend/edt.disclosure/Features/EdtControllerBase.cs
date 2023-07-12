namespace edt.disclosure.Features;
using Microsoft.AspNetCore.Mvc;
using MediatR;

[Produces("application/json")]
[ApiController]
public class EdtControllerBase : ControllerBase
{
    protected readonly IMediator _mediator;


    protected EdtControllerBase(IMediator mediator)
    {
        this._mediator = mediator;
    }

}
