namespace edt.service.Features.Person;

using Common.Models.EDT;
using edt.service.HttpClients.Services.EdtCore;
using MediatR;

public record PersonByIdentifierQuery(string identifierType, string identifierValue) : IRequest<List<EdtPersonDto>>;
public class PersonByIdentifierQueryHandler(IEdtClient edtClient) : IRequestHandler<PersonByIdentifierQuery, List<EdtPersonDto>>
{
    private readonly IEdtClient edtClient = edtClient;

    public Task<List<EdtPersonDto>> Handle(PersonByIdentifierQuery request, CancellationToken cancellationToken)
    {
        Serilog.Log.Information($"Looking up persons by identifiers {request.identifierType} {request.identifierValue}");
        return this.edtClient.GetPersonsByIdentifier(request.identifierType, request.identifierValue);
    }


}
