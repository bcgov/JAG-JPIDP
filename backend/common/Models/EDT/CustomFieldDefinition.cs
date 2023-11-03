namespace Common.Models.EDT;
public class CustomFieldDefinition
{

    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public int Id { get; set; }
    public string ObjectType { get; set; } = string.Empty;
    public int? Order { get; set; }

}
