namespace edt.disclosure.HttpClients.Services.EdtDisclosure;

public class EdtCaseDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string TimeZoneId { get; set; } = "Pacific Standard Time";
    public string TemplateCase { get; set; } = string.Empty;

    public List<CaseField> Fields { get; set; } = new List<CaseField>();
}

public class CaseField
{
    public string Name { get; set; } = string.Empty;
    public object? Value { get; set; }

}
