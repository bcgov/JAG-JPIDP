﻿using jumwebapi.Data.ef;
using jumwebapi.Features.Users.Models;

namespace jumwebapi.Features.Users.Mapping;

public class UserEntityMap : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<JustinUser, UserModel>()
         .Map(dest => dest.UserId, src => src.UserId)
         .Map(dest => dest.UserName, src => src.UserName)
         .Map(dest => dest.IsDisable, src => src.IsDisabled)
         .Map(dest => dest.ParticipantId, src => src.ParticipantId)
         //.Map(dest => dest.DigitalIdentifier, src => src.DigitalIdentifier)
         .Map(dest => dest.FirstName, src => src.Person.FirstName)
        .Map(dest => dest.LastName, src => src.Person.Surname)
        .Map(dest => dest.MiddleName, src => src.Person.MiddleNames)
        .Map(dest => dest.Email, src => src.Person.Email)
        .Map(dest => dest.PhoneNumber, src => src.Person.Phone)
        .Map(dest => dest.BirthDate, src => src.Person.BirthDate)
        .Map(dest => dest.IsDisable, src => src.Person.IsDisabled)
         .Map(dest => dest.AgencyId, src => src.AgencyId)
         .Map(dest => dest.PartyTypeCode, src => src.PartyType.Code)
         .Map(dest => dest.Roles, src => src.UserRoles.Select(r => r.Role));

        config.NewConfig<UserModel, JustinUser>()
           .Map(dest => dest.UserId, src => src.UserId)
           .Map(dest => dest.UserName, src => src.UserName)
           .Map(dest => dest.IsDisabled, src => src.IsDisable)
           .Map(dest => dest.ParticipantId, src => src.ParticipantId)
           .Map(dest => dest.Person.FirstName, src => src.FirstName)
           .Map(dest => dest.Person.Surname, src => src.LastName)
           .Map(dest => dest.Person.MiddleNames, src => src.MiddleName)
           .Map(dest => dest.Person.Email, src => src.Email)
           .Map(dest => dest.Person.Phone, src => src.PhoneNumber)
           .Map(dest => dest.Person.BirthDate, src => src.BirthDate)
           .Map(dest => dest.Person.IsDisabled, src => src.IsDisable)
           .Map(dest => dest.AgencyId, src => src.AgencyId)
           // .Map(dest => dest.DigitalIdentifier, src => src.DigitalIdentifier)
           .Map(dest => dest.PartyType.Code, src => src.PartyTypeCode);
    }
}
