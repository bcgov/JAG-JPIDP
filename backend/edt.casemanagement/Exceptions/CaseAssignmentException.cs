namespace edt.casemanagement.Exceptions;

using System.Runtime.Serialization;

[Serializable]
public class CaseAssignmentException : Exception
{
    public CaseAssignmentException() : base() { }
    public CaseAssignmentException(string message) : base(message ?? "Case assignment failure") { }

    protected CaseAssignmentException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext) { }
}
