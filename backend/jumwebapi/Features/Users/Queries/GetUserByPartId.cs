using jumwebapi.Data.ef;
using jumwebapi.Features.Users.Services;
using MediatR;

namespace jumwebapi.Features.Users.Queries;

public sealed record GetUserByPartId(decimal partId) : IRequest<JustinUser>;
public class GetUserByPartIdHandler : IRequestHandler<GetUserByPartId, JustinUser>
{
    private readonly IUserService _userService;
    public GetUserByPartIdHandler(IUserService userService)
    {
        _userService = userService;
    }
    public async Task<JustinUser> Handle(GetUserByPartId request, CancellationToken cancellationToken)
    {
        return await _userService.GetUserByPartId(request.partId);
    }
}
