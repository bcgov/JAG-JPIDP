using jumwebapi.Data.ef;

namespace jumwebapi.Features.Persons.Services;

public interface IPersonService
{
    Task<IEnumerable<JustinPerson>> AllPerson();
    Task<IEnumerable<JustinPerson>> AllPersonAsync();
    Task<long> CreatePerson(JustinPerson person);
}
