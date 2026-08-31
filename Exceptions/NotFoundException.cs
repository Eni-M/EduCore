namespace EduCore.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }

    public NotFoundException(string resource, object id)
        : base($"{resource} not found with id: {id}") { }
}
