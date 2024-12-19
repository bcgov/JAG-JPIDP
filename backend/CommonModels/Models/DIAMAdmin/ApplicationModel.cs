namespace CommonModels.Models.DIAMAdmin;

using System.ComponentModel;
using NodaTime;

public class ApplicationModel : ApplicationSummaryModel
{

    public List<string> Tags { get; set; } = [];
    // from db
    public List<string> UserTypes { get; set; } = [];
    public List<string> DomainPrefixList { get; set; } = [];
    public List<ApplicationUrlModel> ApplicationUrls { get; set; } = [];
    public List<ApplicationGroupModel> ApplicationGroups { get; set; } = [];
    public List<ApplicationPublishStatusModel> PublishStatusList { get; set; } = [];

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

public class ApplicationPublishStatusModel
{
    public Instant? PublishDate { get; set; }
    public string? PublishNotes { get; set; }
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



