namespace KanbanTracker.Domain.Exceptions;

/// <summary>
/// Базовий клас для всіх доменних винятків.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception inner) : base(message, inner) { }
}
