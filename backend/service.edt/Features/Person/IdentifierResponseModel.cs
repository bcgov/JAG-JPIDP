namespace edt.service.Features.Person;

using Common.Models.EDT;

public class IdentifierResponseModel
{
    public List<IdentifierModel> Items { get; set; } = new List<IdentifierModel>();
    public int Total { get; set; }
}
