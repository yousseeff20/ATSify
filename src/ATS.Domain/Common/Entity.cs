namespace ATS.Domain.Common;

public abstract class Entity : IEquatable<Entity>
{
    public Guid Id { get; protected set; }

    protected Entity() { }

    protected Entity(Guid id)
    {
        Id = id;
    }

    public static bool operator ==(Entity? left, Entity? right) =>
        left is not null && right is not null && left.Equals(right);

    public static bool operator !=(Entity? left, Entity? right) =>
        !(left == right);

    public bool Equals(Entity? other)
    {
        if (other is null || other.GetType() != GetType())
            return false;

        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType())
            return false;

        return ((Entity)obj).Id == Id;
    }

    public override int GetHashCode() => Id.GetHashCode();
}
