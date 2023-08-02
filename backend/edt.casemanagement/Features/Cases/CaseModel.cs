namespace edt.casemanagement.Features.Cases;

public class CaseModelx
{

    public string Name { get; set; } = string.Empty;
    public int Id { get; set; }

    public string Status { get; set; }
    public string Key { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public List<Fieldx> Fields { get; set; }

}
public class Fieldx

{
    public int Id { get; set; }
    public string Name { get; set; }
    public object Value { get; set; }
    public bool Display { get; set; } = true;
}
