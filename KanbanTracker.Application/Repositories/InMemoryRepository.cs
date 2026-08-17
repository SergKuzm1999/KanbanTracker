using KanbanTracker.Domain.Interfaces;

namespace KanbanTracker.Application.Repositories;

/// <summary>
/// Generic in-memory repository (Generics + ISP/DIP).
/// </summary>
public class InMemoryRepository<T> : IRepository<T> where T : class, IEntity
{
    private readonly Dictionary<Guid, T> _store = new();

    public T? GetById(Guid id) => _store.TryGetValue(id, out var e) ? e : null;

    public IEnumerable<T> GetAll() => _store.Values.ToList();

    public void Add(T entity)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        _store[entity.Id] = entity;
    }

    public void Update(T entity)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (!_store.ContainsKey(entity.Id))
            throw new KeyNotFoundException($"Entity {entity.Id} not found.");
        _store[entity.Id] = entity;
    }

    public void Remove(Guid id) => _store.Remove(id);

    public bool Exists(Guid id) => _store.ContainsKey(id);
}
