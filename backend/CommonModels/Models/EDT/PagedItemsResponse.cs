namespace Common.Models.EDT;


public class PagedItemsResponse<T>
{
    public List<T> Items { get; set; } = [];
    public int Total { get; set; }
}
