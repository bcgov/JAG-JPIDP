namespace NotificationService.Exceptions;

using System.Runtime.Serialization;

public class DeliveryException : Exception
{
    public DeliveryException() : base() { }
    public DeliveryException(string message) : base(message ?? "Delivery error") { }

    protected DeliveryException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext) { }
}
