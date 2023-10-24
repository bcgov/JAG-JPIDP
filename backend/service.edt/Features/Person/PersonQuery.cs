namespace edt.service.Features.Person;

using Common.Models.EDT;
using edt.service.HttpClients.Services.EdtCore;
using MediatR;

public record PersonQuery(int personId) : IRequest<EdtPersonDto>;
public class UserQueryHandler : IRequestHandler<PersonQuery, EdtPersonDto>
{
    private readonly IEdtClient edtClient;


    public UserQueryHandler(IEdtClient edtClient)
    {
        this.edtClient = edtClient;
    }

    public Task<EdtPersonDto?> Handle(PersonQuery request, CancellationToken cancellationToken)
    {
        Serilog.Log.Information($"Looking up person {request.personId}");
        return this.edtClient.GetPersonById(request.personId);
    }


}
