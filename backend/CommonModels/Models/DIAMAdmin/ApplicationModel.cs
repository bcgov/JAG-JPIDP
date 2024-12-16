namespace CommonModels.Models.DIAMAdmin;

using System.ComponentModel;

public class ApplicationModel
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Abbreviation { get; set; }
    public required string Description { get; set; }
    public required string SummaryText { get; set; }
    public required string BackgroundColour { get; set; }
    public required string Colour { get; set; }

    public required string Icon { get; set; }
    public string BaseUrl { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    // from db
    public List<string> UserTypes { get; set; } = [];
    public List<string> DomainPrefixList { get; set; } = [];
    public List<ApplicationUrlModel> ApplicationUrls { get; set; } = [];
    public List<ApplicationGroupModel> ApplicationGroups { get; set; } = [];

    public ProjectType ProjectType { get; set; } = ProjectType.JUSTIN;


}

public class ApplicationGroupModel
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class ApplicationUrlModel
{
    public string Url { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}


public enum ProjectType
{
    [Description("JUSTIN")]
    JUSTIN,
    [Description("Corrections")]
    CORNET,
    [Description("Combined JUSTIN and CORNET")]
    JUSTINCORNET,
    [Description("Generic - non JUSTIN or CORNET related")]
    GENERIC

}



