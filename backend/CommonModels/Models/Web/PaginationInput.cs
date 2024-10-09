namespace CommonModels.Models.Web;


public class PaginationInput
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public string? SearchValue { get; set; }
    public Dictionary<string, string> Filters { get; set; } = [];
    public Dictionary<string, int> Sorts { get; set; } = [];
}
