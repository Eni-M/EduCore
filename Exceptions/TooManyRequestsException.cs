namespace EduCore.Exceptions;

public class TooManyRequestsException : Exception
{
    public TooManyRequestsException(string message) : base(message) { }
}
