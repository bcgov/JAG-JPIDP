namespace Common.Models.EDT;
public class CaseModel
{

    public string Name { get; set; } = string.Empty;
    public int Id { get; set; }

    public string Status { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Errors { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public List<Field>? Fields { get; set; }

}
public class Field

{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public object Value { get; set; } = string.Empty;
    public bool Display { get; set; } = true;
}

public enum EDTCaseStatus
{
    Active,
    Inactive,
    NotFound
}
