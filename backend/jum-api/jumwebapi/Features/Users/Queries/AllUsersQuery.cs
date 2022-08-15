﻿using jumwebapi.Data.ef;
using jumwebapi.Features.Users.Services;
using MediatR;

namespace jumwebapi.Features.Users.Queries;

public sealed record AllUsersQuery: IRequest<IEnumerable<JustinUser>>;
public class AllUsersQueryHandler : IRequestHandler<AllUsersQuery, IEnumerable<JustinUser>>
{
    private readonly IUserService _userService;
    public AllUsersQueryHandler(IUserService userService)
    {
        _userService = userService;
    }

    public async Task<IEnumerable<JustinUser>> Handle(AllUsersQuery request, CancellationToken cancellationToken)
    {
        return await _userService.AllUsersList();
    }
}
