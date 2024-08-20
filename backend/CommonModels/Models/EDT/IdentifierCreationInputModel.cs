namespace Common.Models.EDT;


public class IdentifierCreationInputModel
{
    public string EntityType { get; set; } = "Person";
    public string IdentifierValue { get; set; } = string.Empty;
    public string IdentifierType { get; set; } = "EdtExternalId";
    public int EntityId { get; set; }
}
