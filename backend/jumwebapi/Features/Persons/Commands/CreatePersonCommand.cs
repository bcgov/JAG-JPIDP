﻿using AutoMapper;
using jumwebapi.Data.ef;
using jumwebapi.Features.Persons.Models;
using jumwebapi.Features.Persons.Services;
using MediatR;

namespace jumwebapi.Features.Persons.Commands;

public sealed record CreatePersonCommand(string Surname, string FirstName, string MiddleName, string PreferredName, DateTime DateofBirth) : IRequest<long>;
public class CreatePersonCommandHandler : IRequestHandler<CreatePersonCommand, long>
{
    private readonly IPersonService _personService;
    private readonly IMapper _mapper;
    public CreatePersonCommandHandler(IPersonService personService, IMapper mapper)
    {
        _personService = personService;
        _mapper = mapper;
    }

    public async Task<long> Handle(CreatePersonCommand request, CancellationToken cancellationToken)
    {
        var person = new JustinPerson
        {
            Surname = request.Surname,
            FirstName = request.FirstName,
            MiddleNames = request.MiddleName,
            PreferredName = request.PreferredName,
            BirthDate = request.DateofBirth,
            IsDisabled = false
        };
        //var entity = JustinPerson;
        return await _personService.CreatePerson(person);

    }
}

