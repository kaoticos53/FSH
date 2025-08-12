using System.Diagnostics.CodeAnalysis;

namespace FSH.Framework.Core.Audit;

/// <summary>
/// Servicio de estadísticas de auditoría.
/// </summary>
public interface IAuditStatsService
{
    /// <summary>
    /// Obtiene un resumen agregado de auditoría para un rango de fechas.
    /// </summary>
    /// <param name="from">Fecha inicial (inclusive). Si es null, se usa hoy-30.</param>
    /// <param name="to">Fecha final (inclusive). Si es null, se usa hoy.</param>
    /// <param name="topN">Número de elementos superiores para entidades y usuarios.</param>
    Task<AuditStatsSummaryDto> GetSummaryAsync(DateOnly? from, DateOnly? to, int topN = 5, CancellationToken ct = default);

    /// <summary>
    /// Serie temporal de conteos por día para el rango indicado.
    /// </summary>
    /// <param name="from">Fecha inicial (inclusive). Si es null, se usa hoy-30.</param>
    /// <param name="to">Fecha final (inclusive). Si es null, se usa hoy.</param>
    /// <param name="bucket">Agrupación temporal. Por ahora solo "day".</param>
    Task<List<AuditTimeseriesPointDto>> GetTimeseriesAsync(DateOnly? from, DateOnly? to, string bucket = "day", CancellationToken ct = default);
}
