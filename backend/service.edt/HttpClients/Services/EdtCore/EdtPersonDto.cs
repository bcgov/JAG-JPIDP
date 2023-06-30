namespace edt.service.HttpClients.Services.EdtCore;

public class EdtPersonDto
{
    public string? Key { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public EdtPersonAddress? Address { get; set; } = new EdtPersonAddress();
    public string? Role { get; set; } = "Defence";
    public bool? IsActive { get; set; } = true;
    public List<EdtField> Fields { get; set; } = new List<EdtField>();

}

public class EdtPersonAddress
{
    public string? Email { get; set; }
    public string? Mobile { get; set; }
    public string? Phone { get; set; }
}

public class EdtField
{
    public string? Name { get; set; }
    public string? Value { get; set; }
}
