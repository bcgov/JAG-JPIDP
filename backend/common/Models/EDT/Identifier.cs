namespace Common.Models.EDT;


/// <summary>
/// Represents an identifier for an entity in Edt.
/// Typically used to associated additional identifiers to a Person/Participant
/// </summary>
public class Identifier
{
    public int Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string IdentifierType { get; set; } = string.Empty;
    public string IdentifierValue { get; set; } = string.Empty;
    public int EntityId { get; set; }
}
