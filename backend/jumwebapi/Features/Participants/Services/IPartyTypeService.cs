﻿using jumwebapi.Data.ef;

namespace jumwebapi.Features.Participants.Services;

public interface IPartyTypeService
{
    Task<IEnumerable<JustinPartyType>> GetPartyTypeList();
    Task<JustinPartyType> PartyTypeById(int id);
    Task<JustinPartyType> CreatePartyType(JustinPartyType partyType);
    Task<JustinPartyType> UpdatePartyType(JustinPartyType partyType);
    Task<int> DeletePartyType(JustinPartyType partyType);
}
