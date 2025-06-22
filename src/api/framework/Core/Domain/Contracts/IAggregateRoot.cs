namespace FSH.Framework.Core.Domain.Contracts;

/// <summary>
/// Marker interface for aggregate root entities in the domain model.
/// Aggregate roots are the main entities that are loaded from and saved to repositories.
/// They are the only entities that should be referenced by other aggregates.
/// 
/// Apply this marker interface only to aggregate root entities.
/// Repositories will only work with aggregate roots, not their children.
/// </summary>
/// <remarks>
/// An aggregate is a cluster of domain objects that can be treated as a single unit.
/// An aggregate root is the root entity of the aggregate and is the only member of the
/// aggregate that outside objects are allowed to hold references to.
/// </remarks>
public interface IAggregateRoot : IEntity
{
}
