namespace edt.disclosure.Exceptions;

public class ResourceNotFoundException : Exception
{
    public ResourceNotFoundException(string type, string name) : base($"Resource {type} - {name} not found") { }

}
