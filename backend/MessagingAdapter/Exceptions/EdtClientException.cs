namespace MessagingAdapter.Exceptions;

public class EdtClientException : Exception
{
    public EdtClientException() : base() { }
    public EdtClientException(string message) : base(message ?? "EDT Service not responding") { }
}
