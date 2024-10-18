namespace edt.service.Features.Users;

using System.Threading;
using System.Threading.Tasks;
using Common.Models.EDT;
using edt.service.HttpClients.Services.EdtCore;
using MediatR;

public record UserCasesQuery(string UserId) : IRequest<List<UserCaseSearchResponseModel>>;

public class UserCasesQueryHandler(IEdtClient edtClient, ILogger<UserCasesQuery> logger) : IRequestHandler<UserCasesQuery, List<UserCaseSearchResponseModel>>
{
    public async Task<List<UserCaseSearchResponseModel>> Handle(UserCasesQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation($"Looking up user cases for user {request.UserId}");

        return await edtClient.GetUserCases(request.UserId);

    }
}
