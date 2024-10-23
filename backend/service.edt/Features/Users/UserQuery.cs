namespace edt.service.Features.Users;

using System.Threading;
using Common.Models.EDT;
using edt.service.HttpClients.Services.EdtCore;
using MediatR;

public record UserQuery(string userKey) : IRequest<EdtUserDto>;

public class UserQueryHandler : IRequestHandler<UserQuery, EdtUserDto>
{
    private readonly IEdtClient edtClient;


    public UserQueryHandler(IEdtClient edtClient)
    {
        this.edtClient = edtClient;
    }

    public Task<EdtUserDto> Handle(UserQuery request, CancellationToken cancellationToken)
    {
        Serilog.Log.Information($"Looking up user {request.userKey}");
        return this.edtClient.GetUser(request.userKey);
    }


}
