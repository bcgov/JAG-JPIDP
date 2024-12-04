namespace CommonModels.Models.Web;

using System.Text.Json.Serialization;

public class TreeNode
{
    public int Id { get; set; }
    public int ParentId { get; set; }
    public required string Name { get; set; }
    public string? InternalId { get; set; }
    public int Level { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Icon { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<TreeNode> Children { get; set; } = [];
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ClassName { get; set; }
}
