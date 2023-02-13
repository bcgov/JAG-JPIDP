using jumwebapi.Data.ef;

namespace jumwebapi.Features.Agencies.Services;

public interface IAgencyService
{
    Task<IEnumerable<JustinAgency>> GetAllAgencies();
    Task<JustinAgency> AgencyById(long id);
    Task<JustinAgency> AddAgency(JustinAgency agency);
    Task<JustinAgency> UpdateAgency(JustinAgency agency);
    Task<long> DeleteAgency(JustinAgency agency);
}
