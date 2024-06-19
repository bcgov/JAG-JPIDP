namespace DIAMCornetService.Exceptions;

public class CornetException : Exception
{
    public CornetException()
    {
    }

    public CornetException(string message)
        : base(message)
    {
    }

    public CornetException(string message, Exception inner)
        : base(message, inner)
    {
    }

}
