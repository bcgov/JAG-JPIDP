namespace edt.service.Features.Person;

using Common.Models.EDT;
using edt.service.HttpClients.Services.EdtCore;
using MediatR;


public record PersonSearchQuery(PersonLookupModel PersonLookup) : IRequest<List<EdtPersonDto>>;
public class PersonSearchQueryHandler(IEdtClient edtClient) : IRequestHandler<PersonSearchQuery, List<EdtPersonDto>>
{

    public Task<List<EdtPersonDto>> Handle(PersonSearchQuery request, CancellationToken cancellationToken)
    {
        Serilog.Log.Information($"Searching for people: {request}");

        return edtClient.SearchForPerson(request.PersonLookup);

    }


}

