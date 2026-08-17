namespace KanbanTracker.Domain.Interfaces;

/// <summary>
/// Універсальне сховище (Generics + DIP).
/// </summary>
public interface IRepository<T> where T : class, IEntity
{
    T? GetById(Guid id);
    IEnumerable<T> GetAll();
    void Add(T entity);
    void Update(T entity);
    void Remove(Guid id);
    bool Exists(Guid id);
}
