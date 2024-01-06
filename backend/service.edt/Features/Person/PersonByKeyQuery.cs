namespace edt.service.Features.Person;

using Common.Models.EDT;
using edt.service.HttpClients.Services.EdtCore;
using MediatR;

public record PersonByKeyQuery(string key) : IRequest<EdtPersonDto>;
public class UserQueryByKeyHandler : IRequestHandler<PersonByKeyQuery, EdtPersonDto>
{
    private readonly IEdtClient edtClient;


    public UserQueryByKeyHandler(IEdtClient edtClient)
    {
        this.edtClient = edtClient;
    }

    public Task<EdtPersonDto?> Handle(PersonByKeyQuery request, CancellationToken cancellationToken)
    {
        Serilog.Log.Information($"Looking up person by key {request.key}");
        return this.edtClient.GetPerson(request.key);
    }


}
