namespace CommonModels.Models.Web;


public class PaginatedInputWrapper(PaginationInput input)
{
    public PaginationInput Input { get; set; } = input;
}
