namespace edt.disclosure.Features.Defence;

using Common.Models.EDT;
using edt.disclosure.HttpClients.Services.EdtDisclosure;
using MediatR;


public record CaseQuery(string fieldName, string fieldValue) : IRequest<CaseModel>;

public class CaseLookupQueryHandler : IRequestHandler<CaseQuery, CaseModel>
{

    private readonly IEdtDisclosureClient edtClient;

    public CaseLookupQueryHandler(IEdtDisclosureClient edtClient) => this.edtClient = edtClient;

    public async Task<CaseModel> Handle(CaseQuery request, CancellationToken cancellationToken) => await this.edtClient.FindCase(request.fieldName, request.fieldValue);
}
