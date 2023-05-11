namespace edt.disclosure.ServiceEvents.Models;

using NodaTime;

public abstract class BaseAuditable
{
    public Instant Created { get; set; }
    public Instant Modified { get; set; }
}
