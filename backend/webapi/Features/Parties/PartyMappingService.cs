namespace Pidp.Features.Parties;

using CommonModels.Models.Party;
using Pidp.Models;

public class PartyMappingService
{


    public static PartyDetailModel MapToDTO(Party party)
    {
        return new PartyDetailModel
        {
            Jpdid = party.Jpdid,
            UserId = party.UserId,
            Email = party.Email,
            FirstName = party.FirstName,
            Id = party.Id,
            LastName = party.LastName,
            Birthdate = party.Birthdate

        };
    }

    public static List<PartyDetailModel> MapToDTO(List<Party> parties) => parties.Select(MapToDTO).ToList();

}
