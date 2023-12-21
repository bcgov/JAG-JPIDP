namespace Common.Models.EDT;

public class EdtField
{
    public int Id { get; set; }
    public string? Name { get; set; } = string.Empty;
    public object? Value { get; set; }
}

public class ValueField
{
    public int Id { get; set; }
    public string? Name { get; set; } = string.Empty;
}
