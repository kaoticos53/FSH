using System.Collections.ObjectModel;
using System.Text.Json;

namespace FSH.Framework.Core.Audit;

/// <summary>
/// Represents a Data Transfer Object (DTO) for audit trail information.
/// This class is used to collect and transfer audit data before it's persisted as an <see cref="AuditTrail"/>.
/// </summary>
public class TrailDto()
{
    /// <summary>
    /// Gets or sets the unique identifier for this audit trail entry.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the audited operation occurred.
    /// </summary>
    public DateTimeOffset DateTime { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who performed the audited operation.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets the dictionary of key-value pairs representing the primary key values of the audited entity.
    /// </summary>
    public Dictionary<string, object?> KeyValues { get; } = [];

    /// <summary>
    /// Gets the dictionary of property names and their original values before the operation.
    /// </summary>
    public Dictionary<string, object?> OldValues { get; } = [];

    /// <summary>
    /// Gets the dictionary of property names and their new values after the operation.
    /// </summary>
    public Dictionary<string, object?> NewValues { get; } = [];

    /// <summary>
    /// Gets the collection of property names that were modified during the operation.
    /// </summary>
    public Collection<string> ModifiedProperties { get; } = [];

    /// <summary>
    /// Gets or sets the type of the audited operation (e.g., Create, Update, Delete).
    /// </summary>
    public TrailType Type { get; set; }

    /// <summary>
    /// Gets or sets the name of the database table where the audited entity is stored.
    /// </summary>
    public string? TableName { get; set; }


    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false,
    };

    /// <summary>
    /// Converts this DTO to an <see cref="AuditTrail"/> entity for persistence.
    /// </summary>
    /// <returns>A new <see cref="AuditTrail"/> instance populated with data from this DTO.</returns>
    public AuditTrail ToAuditTrail()
    {
        return new()
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            Operation = Type.ToString(),
            Entity = TableName,
            DateTime = DateTime,
            PrimaryKey = JsonSerializer.Serialize(KeyValues, SerializerOptions),
            PreviousValues = OldValues.Count == 0 ? null : JsonSerializer.Serialize(OldValues, SerializerOptions),
            NewValues = NewValues.Count == 0 ? null : JsonSerializer.Serialize(NewValues, SerializerOptions),
            ModifiedProperties = ModifiedProperties.Count == 0 ? null : JsonSerializer.Serialize(ModifiedProperties, SerializerOptions)
        };
    }
}
