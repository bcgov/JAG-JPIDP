namespace jumwebapi.Models
{
    public interface IOwnedResource
    {
        Guid UserId { get; set; }
    }
}
