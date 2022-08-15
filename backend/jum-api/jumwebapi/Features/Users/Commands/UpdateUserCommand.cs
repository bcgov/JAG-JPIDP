﻿using FluentValidation;
using jumwebapi.Data.ef;
using jumwebapi.Features.Roles.Models;
using jumwebapi.Features.Users.Models;
using jumwebapi.Features.Users.Services;
using MapsterMapper;
using MediatR;

namespace jumwebapi.Features.Users.Commands;


public sealed record UpdateUserCommand(long UserId,
    string UserName,
    long ParticipantId,
    // Guid DigitalIdentifier,
    bool IsDisable,
    string FirstName,
    string LastName,
    string MiddleName,
    string PreferredName,
    string PhoneNumber,
    string Email,
    DateTime BirthDate,
    long AgencyId,
    PartyTypeCode PartyTypeCode,
    IEnumerable<RoleModel> Roles) : IRequest<UserModel>;
public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UserModel>
{
    private readonly IUserService _userService;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateUserCommand> _validator;
    public UpdateUserCommandHandler(IUserService userService, IMapper mapper, IValidator<CreateUserCommand> validator)
    {
        _userService = userService;
        _mapper = mapper;
        _validator = validator;
    }

    public async Task<UserModel> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
       // _validator.ValidateAndThrow(request);

        var entity = _mapper.Map<JustinUser>(request);

        var user = await _userService.UpdateUser(entity);

        return _mapper.Map<UserModel>(user);
    }
}
