namespace Pidp.Features.Parties;

using Common.Models.EDT;

public interface IProfileUpdateService
{
    public Task<bool> UpdateUserProfile(UpdatePersonContactInfoModel updatePerson);
}
