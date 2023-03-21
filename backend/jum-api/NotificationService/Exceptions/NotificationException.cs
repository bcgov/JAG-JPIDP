namespace NotificationService.Exceptions;

public class NotificationException : Exception
{
    public NotificationException() : base() { }
    public NotificationException(string message) : base(message ?? "Notification service error") { }

}
