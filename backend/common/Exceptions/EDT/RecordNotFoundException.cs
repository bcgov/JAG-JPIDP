namespace Common.Exceptions;

public class RecordNotFoundException : Exception
{
    public RecordNotFoundException() : base() { }
    public RecordNotFoundException(string type, string key) : base($"EDT record [{type}:{key}] not found") { }
    public RecordNotFoundException(string message) : base(message ?? "Participant Lookup Error") { }
}
