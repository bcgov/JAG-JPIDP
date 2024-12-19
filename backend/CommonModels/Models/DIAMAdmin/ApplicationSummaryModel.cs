namespace CommonModels.Models.DIAMAdmin;


public class ApplicationSummaryModel
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
}
