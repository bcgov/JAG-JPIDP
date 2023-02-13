using jumwebapi.Data.ef;
using jumwebapi.Features.Persons.Services;
using MediatR;

namespace jumwebapi.Features.Persons.Queries;

public sealed record PersonQuery : IRequest<IEnumerable<JustinPerson>>;
public class PersonQueryHandler : IRequestHandler<PersonQuery, IEnumerable<JustinPerson>>
{
    private IPersonService _personService;
    public PersonQueryHandler(IPersonService personService)
    {
        _personService = personService;
    }

    public async Task<IEnumerable<JustinPerson>> Handle(PersonQuery request, CancellationToken cancellationToken)
    {
        return await _personService.AllPersonAsync();
    }
}
