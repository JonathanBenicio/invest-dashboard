using System;
using System.Collections.Generic;

namespace InvestDashboard.Domain.Common;

public abstract class Entity<TId>
{
    public TId Id { get; protected set; }

    protected Entity(TId id)
    {
        if (id == null || EqualityComparer<TId>.Default.Equals(id, default))
            throw new ArgumentException("Id cannot be empty or default", nameof(id));

        Id = id;
    }

    // Required for EF Core / deserialization
#pragma warning disable CS8618 
    protected Entity() { }
#pragma warning restore CS8618

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (Id is null || other.Id is null)
            return false;

        if (EqualityComparer<TId>.Default.Equals(Id, default) || EqualityComparer<TId>.Default.Equals(other.Id, default))
            return false;

        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode()
    {
        return Id is null ? 0 : EqualityComparer<TId>.Default.GetHashCode(Id);
    }

    public static bool operator ==(Entity<TId>? a, Entity<TId>? b)
    {
        if (a is null && b is null)
            return true;

        if (a is null || b is null)
            return false;

        return a.Equals(b);
    }

    public static bool operator !=(Entity<TId>? a, Entity<TId>? b)
    {
        return !(a == b);
    }
}
