namespace Common.Models.EDT;

using System.ComponentModel.DataAnnotations;

public class PersonLookupModel
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "Last Name is required")]
    public string? LastName { get; set; }
    [Required(AllowEmptyStrings = false, ErrorMessage = "First Name is required")]
    public string? FirstName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public List<LookupKeyValueGroup> AttributeValues { get; set; } = new List<LookupKeyValueGroup>();
    public bool IncludeInactive { get; set; }

    public bool IsValid()
    {
        var valid = true;
        if (string.IsNullOrEmpty(this.LastName))
        {
            valid = false;
        }
        if (string.IsNullOrEmpty(this.FirstName))
        {
            valid = false;
        }

        return valid;
    }

    public override string ToString() => $"LastName: {this.LastName}, FirstName: {this.FirstName}, DateOfBirth: {this.DateOfBirth?.ToString() ?? "N/A"}, IncludeInactive: {this.IncludeInactive}, AttributeValues: [{string.Join(", ", this.AttributeValues)}]";
}

// can contain additional lookup values/
// e.g Type = Attribute, Name = OTC, Value = 3213
public class LookupKeyValueGroup
{
    public LookupType ValType { get; set; }
    public string? Name { get; set; }
    public string? Value { get; set; }
    public override string ToString() => $"ValType: {this.ValType}, Name: {this.Name}, Value: {this.Value}";
}

// lookups can be either a custom attribute (e.g. OTC) or a fixed EDT field (e.g. EdtExternalId)
public enum LookupType
{
    Attribute,
    Field
}
