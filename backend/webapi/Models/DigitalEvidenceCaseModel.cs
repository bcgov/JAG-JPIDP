namespace Pidp.Models;

/// <summary>
/// Represents a case from another system (e.g. EDT DEMS)
/// </summary>
public class DigitalEvidenceCaseModel
{


    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public List<Field> Fields { get; set; }

}
public class Field

{
    public int Id { get; set; }
    public string Name { get; set; }
    public object Value { get; set; }
}
