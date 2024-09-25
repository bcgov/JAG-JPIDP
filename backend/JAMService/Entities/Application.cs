namespace JAMService.Entities;

using System.ComponentModel.DataAnnotations;

public class Application
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string GroupPath { get; set; } = string.Empty;
}
