namespace edt.service.Features.Cases;

using edt.service.HttpClients.Services.EdtCore;
using MediatR;

public record CaseLookupQuery(string caseName) : IRequest<CaseModel>;

public class CaseLookupQueryHandler : IRequestHandler<CaseLookupQuery, CaseModel>
{

    private readonly IEdtClient edtClient;

    public CaseLookupQueryHandler(IEdtClient edtClient)
    {
        this.edtClient = edtClient;
    }

    public async Task<CaseModel> Handle(CaseLookupQuery request, CancellationToken cancellationToken)
    {

        return await this.edtClient.FindCase(request.caseName);

    }
}
