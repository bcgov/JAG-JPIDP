namespace Pidp.Infrastructure.Services;

using CommonModels.Models.DIAMAdmin;

public interface IDIAMAdminService
{
    public Task<bool> ProcessAdminRequestAsync(AdminRequestModel request);
}
