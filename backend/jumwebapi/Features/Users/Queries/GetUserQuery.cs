using jumwebapi.Data.ef;
using jumwebapi.Features.Users.Services;
using MediatR;

namespace jumwebapi.Features.Users.Queries;


public sealed record GetUserQuery(string username) : IRequest<JustinUser>;
public class GetUserQueryHandler : IRequestHandler<GetUserQuery, JustinUser>
{
    private readonly IUserService _userService;
    public GetUserQueryHandler(IUserService userService)
    {
        _userService = userService;
    }
    public async Task<JustinUser> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        return await _userService.GetUserByUserName(request.username);
    }
}
