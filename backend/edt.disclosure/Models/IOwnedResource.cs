namespace EdtDisclosureService.Models;

public interface IOwnedResource
{
    Guid UserId { get; set; }
}
