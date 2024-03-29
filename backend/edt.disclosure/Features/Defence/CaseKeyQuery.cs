namespace edt.disclosure.Features.Defence;

using Common.Models.EDT;
using edt.disclosure.HttpClients.Services.EdtDisclosure;
using MediatR;


public record CaseKeyQuery(string Key, bool includeFields) : IRequest<CaseModel>;

public class CaseKeyLookupQueryHandler : IRequestHandler<CaseKeyQuery, CaseModel>
{

    private readonly IEdtDisclosureClient edtClient;

    public CaseKeyLookupQueryHandler(IEdtDisclosureClient edtClient) => this.edtClient = edtClient;

    public async Task<CaseModel> Handle(CaseKeyQuery request, CancellationToken cancellationToken) => await this.edtClient.FindCaseByKey(request.Key, request.includeFields);
}


