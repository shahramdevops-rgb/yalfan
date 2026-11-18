namespace Yalfan.Domain.Common;

/// <summary>
/// Base of every entity: something with an identity that stays the same while its data changes.
/// Two entities are equal when they are the same type and have the same <see cref="Id"/>.
/// </summary>
public abstract class Entity : IEquatable<Entity>
{
    /// <summary>
    /// Creates an entity with a new, time-ordered id. The id exists before the entity is saved,
    /// so related rows can point at it in the same transaction. The timestamp inside the id only
    /// keeps the database index compact; it is never read as a business time.
    /// </summary>
    protected Entity()
        : this(Guid.CreateVersion7())
    {
    }

    protected Entity(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("An entity id cannot be empty.", nameof(id));
        }

        Id = id;
    }

    public Guid Id { get; private init; }

    public bool Equals(Entity? other) =>
        other is not null && other.GetType() == GetType() && other.Id == Id;

    public override bool Equals(object? obj) => Equals(obj as Entity);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity? left, Entity? right) => Equals(left, right);

    public static bool operator !=(Entity? left, Entity? right) => !Equals(left, right);
}
