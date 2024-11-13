namespace jumwebapi.Features.Participants.Models;

using System.ComponentModel.DataAnnotations;

public class QueryModel
{
    [Required]
    public string username { get; set; } = string.Empty;
    //public string PartId { get; set; } = string.Empty;
}

