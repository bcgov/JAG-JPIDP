namespace EdtService.Models;

public interface IOwnedResource
{
    Guid UserId { get; set; }
}
