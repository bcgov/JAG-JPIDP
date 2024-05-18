namespace Common.Models.EDT;

using System.ComponentModel.DataAnnotations;

public class PersonLookupModel
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "Last Name is required")]
    public string? LastName { get; set; }
    [Required(AllowEmptyStrings = false, ErrorMessage = "First Name is required")]
    public string? FirstName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public List<LookupKeyValueGroup> AttributeValues { get; set; } = [];

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
}

// can contain additional lookup values/
// e.g Type = Attribute, Name = OTC, Value = 3213
public class LookupKeyValueGroup
{
    public LookupType ValType { get; set; }
    public string? Name { get; set; }
    public string? Value { get; set; }
}

// lookups can be either a custom attribute (e.g. OTC) or a fixed EDT field (e.g. EdtExternalId)
public enum LookupType
{
    Attribute,
    Field
}
