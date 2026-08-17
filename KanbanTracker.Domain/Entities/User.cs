using KanbanTracker.Domain.Exceptions;
using KanbanTracker.Domain.Interfaces;

namespace KanbanTracker.Domain.Entities;

/// <summary>
/// Користувач системи. Демонструє інкапсуляцію та валідацію.
/// </summary>
public class User : IEntity, IDisposable
{
    private string _name = string.Empty;
    private string _email = string.Empty;
    private bool _disposed;

    public Guid Id { get; private set; }

    public string Name
    {
        get => _name;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ValidationException(nameof(Name), "Name cannot be empty.");
            if (value.Length > 100)
                throw new ValidationException(nameof(Name), "Name is too long (max 100).");
            _name = value.Trim();
        }
    }

    public string Email
    {
        get => _email;
        set
        {
            if (string.IsNullOrWhiteSpace(value) || !value.Contains('@'))
                throw new ValidationException(nameof(Email), "Invalid email format.");
            _email = value.Trim().ToLowerInvariant();
        }
    }

    public DateTime CreatedAt { get; private set; }

    // Основний конструктор
    public User(string name, string email)
    {
        Id = Guid.NewGuid();
        Name = name;
        Email = email;
        CreatedAt = DateTime.UtcNow;
    }

    // Конструктор з параметрами (для десеріалізації / відновлення)
    public User(Guid id, string name, string email, DateTime createdAt)
    {
        Id = id;
        Name = name;
        Email = email;
        CreatedAt = createdAt;
    }

    // Копіювальний конструктор
    public User(User other)
    {
        if (other is null) throw new ArgumentNullException(nameof(other));
        Id = Guid.NewGuid(); // нова сутність
        Name = other.Name;
        Email = other.Email;
        CreatedAt = DateTime.UtcNow;
    }

    public override string ToString() => $"{Name} <{Email}>";

    public override bool Equals(object? obj)
    {
        if (obj is User other) return Id == other.Id;
        return false;
    }

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(User? left, User? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        return left.Id == right.Id;
    }

    public static bool operator !=(User? left, User? right) => !(left == right);

    public void Dispose()
    {
        if (_disposed) return;
        // Тут можна звільняти ресурси, якщо були
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~User()
    {
        Dispose();
    }
}
