namespace edt.casemanagement.Features.Cases;

using edt.casemanagement.HttpClients.Services.EdtCore;
using MediatR;

public record CaseLookupQuery(string caseName) : IRequest<CaseModel>;
public record CaseGetByIdQuery(int caseId) : IRequest<CaseModel>;


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

public class CaseGetByIdQueryHandler : IRequestHandler<CaseGetByIdQuery, CaseModel>
{

    private readonly IEdtClient edtClient;

    public CaseGetByIdQueryHandler(IEdtClient edtClient)
    {
        this.edtClient = edtClient;
    }

    public async Task<CaseModel> Handle(CaseGetByIdQuery request, CancellationToken cancellationToken)
    {

        return await this.edtClient.GetCase(request.caseId);


    }
}
