namespace JAMService.Entities;

using System.ComponentModel.DataAnnotations;

public class AppRequest
{
    [Key]
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string AppRequestType { get; set; } = string.Empty;
    public int AppRequestId { get; set; }

}
