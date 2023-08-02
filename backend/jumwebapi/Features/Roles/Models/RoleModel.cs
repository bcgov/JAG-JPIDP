using jumwebapi.Data.ef;
using jumwebapi.Models;

namespace jumwebapi.Features.Roles.Models;

public class RoleModel
{
    public long Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool IsPublic { get; set; }
    public bool IsDisable {get;set;}
}
