namespace KanbanTracker.Domain.Exceptions;

public class ValidationException : DomainException
{
    public string PropertyName { get; }

    public ValidationException(string propertyName, string message)
        : base($"Validation failed for '{propertyName}': {message}")
    {
        PropertyName = propertyName;
    }
}
