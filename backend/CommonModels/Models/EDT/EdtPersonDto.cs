namespace Common.Models.EDT;

using System.Text.Json.Serialization;

public class EdtPersonDto
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int? Id { get; set; }
    public string? Key { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public EdtPersonAddress? Address { get; set; } = new EdtPersonAddress();
    public string? Role { get; set; } = "Defence";
    public bool IsActive { get; set; } = true;
    public string? Status { get; set; }
    public List<EdtField> Fields { get; set; } = [];
    public List<IdentifierModel> Identifiers { get; set; } = [];

    public override string ToString() => $"Id: {this.Id}, Key: {this.Key}, FirstName: {this.FirstName}, LastName: {this.LastName}, Address: {this.Address}, Role: {this.Role}, IsActive: {this.IsActive}, Status: {this.Status}, Fields: {this.Fields.Count}, Identifiers: {this.Identifiers.Count}";
}

public class EdtPersonAddress
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int? Id { get; set; }
    public string? Email { get; set; }
    public string? Mobile { get; set; }
    public string? Phone { get; set; }
}


