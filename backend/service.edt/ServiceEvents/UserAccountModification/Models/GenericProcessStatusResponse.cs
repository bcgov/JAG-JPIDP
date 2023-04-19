namespace edt.service.ServiceEvents.UserAccountModification.Models;

using NodaTime;


/// <summary>
/// Represents a feedback item for a process response
/// </summary>
public class GenericProcessStatusResponse
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<string> ErrorList { get; set; } =  new List<string>();

    public string DomainEvent { get; set; } = string.Empty;
    public Instant EventTime { get; set; }

    public string TraceId { get; set; } = string.Empty;

}
