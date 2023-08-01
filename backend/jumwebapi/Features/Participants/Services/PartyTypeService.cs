﻿using jumwebapi.Data;
using jumwebapi.Data.ef;
using jumwebapi.Models.Lookups;
using Microsoft.EntityFrameworkCore;

namespace jumwebapi.Features.Participants.Services
{
    public class PartyTypeService : IPartyTypeService
    {
        private readonly JumDbContext _context;
        public PartyTypeService(JumDbContext context)
        {
            _context = context;
        }

        public Task<JustinPartyType> CreatePartyType(JustinPartyType partyType)
        {
            throw new NotImplementedException();
        }

        public Task<int> DeletePartyType(JustinPartyType partyType)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<JustinPartyType>> GetPartyTypeList()
        {
            return await _context.PartyTypes.ToListAsync();
        }

        public Task<JustinPartyType> PartyTypeById(int id)
        {
            throw new NotImplementedException();
        }

        public Task<JustinPartyType> UpdatePartyType(JustinPartyType partyType)
        {
            throw new NotImplementedException();
        }
    }
}
