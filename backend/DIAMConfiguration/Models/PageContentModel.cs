namespace DIAMConfiguration.Models;

public class PageContentModel
{
    public string PageId { get; set; } = string.Empty;
    public IList<ContentModel> Contents { get; set; } = new List<ContentModel>();
}

public class ContentModel
{
    public int Id { get; set; }
    public string ContentKey { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
