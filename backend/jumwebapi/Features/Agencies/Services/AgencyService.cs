using jumwebapi.Data;
using jumwebapi.Data.ef;
using Microsoft.EntityFrameworkCore;

namespace jumwebapi.Features.Agencies.Services;

public class AgencyService : IAgencyService
{
    private readonly JumDbContext _context;
    public AgencyService(JumDbContext context)
    {
        _context = context;
    }
    
    public Task<JustinAgency> AddAgency(JustinAgency agency)
    {
        _context.Agencies.Add(agency);
        _context.SaveChanges();
        return Task.FromResult(agency);
    }

    public async Task<JustinAgency> AgencyById(long id)
    {
        return await _context.Agencies.FirstOrDefaultAsync(n => n.AgencyId == id);
    }

    public Task<long> DeleteAgency(JustinAgency agency)
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<JustinAgency>> GetAllAgencies()
    {
        return await _context.Agencies.Include(n=>n.Users).ToListAsync();
    }

    public Task<JustinAgency> UpdateAgency(JustinAgency agency)
    {
        _context.Agencies.Update(agency);
        _context.SaveChanges();
        return Task.FromResult(agency);
    }
}
