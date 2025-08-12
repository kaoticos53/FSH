namespace FSH.Framework.Core.Audit;

/// <summary>
/// DTOs para exponer estadísticas de auditoría (Audit Trail) a la capa de presentación/API.
/// </summary>
public sealed record AuditStatsSummaryDto(
    int Total,
    Dictionary<string, int> ByOperation,
    List<EntityCountDto> TopEntities,
    List<UserCountDto> TopUsers
);

/// <summary>
/// Par (Entidad, Conteo) para clasificaciones.
/// </summary>
public sealed record EntityCountDto(string Entity, int Count);

/// <summary>
/// Par (Usuario, Conteo) para clasificaciones.
/// </summary>
public sealed record UserCountDto(Guid UserId, int Count);

/// <summary>
/// Punto de serie temporal de auditoría.
/// </summary>
public sealed record AuditTimeseriesPointDto(DateOnly Date, int Count);
