namespace CommonModels.Models.Web;

using System.Text.Json.Serialization;

/// <summary>
/// Represents a tree of data
/// </summary>
public class WebTreeView : TreeNode
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Path { get; set; }



    public bool Selected { get; set; }

    public bool Expanded { get; set; }


}
