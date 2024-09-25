namespace JAMService.Entities;

using System.ComponentModel.DataAnnotations;

public class AppRoleMapping
{
    [Key]
    public int Id { get; set; }
    public Application? Application { get; set; }
    public int ApplicationId { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsRealmGroup { get; set; }

}
