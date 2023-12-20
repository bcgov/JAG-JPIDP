namespace Common.Models.EDT;

public class EdtField
{
    public string? Name { get; set; }
    public int? Id { get; set; }
    public List<ValueField> Value { get; set; }
}

public class ValueField
{
    public int Id { get; set; }
    public string? Value { get; set; } = String.Empty;
    public string? Name { get;set; } = String.Empty; 
}
