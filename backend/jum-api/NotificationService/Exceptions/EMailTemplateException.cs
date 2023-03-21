namespace NotificationService.Exceptions;

using System.Runtime.Serialization;

public class EMailTemplateException : Exception
{
    public EMailTemplateException() : base() { }
    public EMailTemplateException(string message) : base(message ?? "EMail template format error") { }

    protected EMailTemplateException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext) { }
}
