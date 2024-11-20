namespace CommonModels.Models.Web;

using System.Text.Json.Serialization;

/// <summary>
/// Represents a tree of data
/// </summary>
public class WebTreeView
{
    public int Id { get; set; }
    public int ParentId { get; set; }
    public required string Name { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Path { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ClassName { get; set; }

    public bool Selected { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Icon { get; set; }

    public bool Expanded { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<WebTreeView> Children { get; set; } = [];
}
