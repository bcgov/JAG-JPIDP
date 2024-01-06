namespace Pidp.Models;

public class UserValidationResponse
{
    public int PartyId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool Validated { get; set; }
    public bool TooManyAttempts { get; set; }
    public bool AlreadyActive { get; set; }
    public string RequestStatus { get; set; } = string.Empty;
    public bool DataMismatch { get; set; }
}

