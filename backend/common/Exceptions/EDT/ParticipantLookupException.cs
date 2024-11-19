namespace Common.Exceptions.EDT;

public class ParticipantLookupException : Exception
{
    public ParticipantLookupException() : base() { }
    public ParticipantLookupException(string message) : base(message ?? "Participant Lookup Error") { }
}
