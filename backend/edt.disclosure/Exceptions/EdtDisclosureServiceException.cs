namespace edt.disclosure.Exceptions;

using System.Runtime.Serialization;

[Serializable]
public class EdtDisclosureServiceException : Exception
{
    public EdtDisclosureServiceException() : base() { }
    public EdtDisclosureServiceException(string message) : base(message ?? "EDT Disclosure Service not responding") { }

    protected EdtDisclosureServiceException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext) { }

}
