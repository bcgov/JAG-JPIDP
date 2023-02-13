namespace jumwebapi.Features.Participants.Models;
public class QueryModel
{
    [Required]
    public string username { get; set; } = string.Empty;
    //public string PartId { get; set; } = string.Empty;
}

