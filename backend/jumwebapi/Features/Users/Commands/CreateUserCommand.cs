namespace jumwebapi.Features.Users.Commands;

using jumwebapi.Data.ef;
using jumwebapi.Features.Roles.Models;
using jumwebapi.Features.Users.Services;
using MediatR;
using jumwebapi.Features.Users.Models;
using FluentValidation;
using AutoMapper;

public sealed record CreateUserCommand(
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

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserModel>
{
    private readonly IUserService _userService;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateUserCommand> _validator;
    public CreateUserCommandHandler(IUserService userService,
                                    IMapper mapper,
                                    IValidator<CreateUserCommand> validator)
    {
        _userService = userService;
        _mapper = mapper;
        _validator = validator;
        
    }

    public async Task<UserModel> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        _validator.ValidateAndThrow(request);

        var entity = _mapper.Map<JustinUser>(request);

        var user = await _userService.AddUser(entity);

        return _mapper.Map<UserModel>(user);
    }
}

