using NodaTime;

namespace jumwebapi.Models;
public abstract class BaseAuditable
{
    public Instant Created { get; set; }
    public Instant Modified { get; set; }
}
