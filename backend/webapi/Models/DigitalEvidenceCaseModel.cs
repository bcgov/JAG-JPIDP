namespace Pidp.Models;

using NodaTime;

/// <summary>
/// Represents a case from another system (e.g. EDT DEMS)
/// </summary>
public class DigitalEvidenceCaseModel
{


    public int Id { get; set; }
    public int RequestId { get; set; }

    public int PartyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AgencyFileNumber { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;

    public Instant RequestedOn { get; set; }
    public Instant AssignedOn { get; set; }
    public Instant LastUpdated { get; set; }
    public string Status { get; set; }

    public string RequestStatus { get; set; } = string.Empty;

    public List<Field> Fields { get; set; }

}
public class Field

{
    public int Id { get; set; }
    public string Name { get; set; }
    public object Value { get; set; }

    public bool Display { get; set; }
}
