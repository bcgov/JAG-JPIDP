namespace Pidp.Exceptions;

[Serializable]
public class AccessRequestException : Exception
{
    public AccessRequestException() : base() { }
    public AccessRequestException(string message) : base(message ?? "Invalid access request.") { }
}
