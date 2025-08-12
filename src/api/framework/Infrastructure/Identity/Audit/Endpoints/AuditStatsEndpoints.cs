using FSH.Framework.Core.Audit;
using FSH.Framework.Infrastructure.Auth.Policy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Framework.Infrastructure.Identity.Audit.Endpoints;

/// <summary>
/// Endpoints para exponer estadísticas de auditoría en la API.
/// </summary>
public static class AuditStatsEndpoints
{
    internal static IEndpointRouteBuilder MapAuditStatsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/summary", (
            DateOnly? from,
            DateOnly? to,
            int? topN,
            IAuditStatsService service,
            CancellationToken ct) => service.GetSummaryAsync(from, to, topN ?? 5, ct))
            .WithName("GetAuditStatsSummary")
            .WithSummary("Obtiene resumen agregado de auditoría")
            .WithDescription("Devuelve totales y clasificaciones por operación, entidad y usuario para el rango indicado.")
            .RequirePermission("Permissions.AuditTrails.View");

        endpoints.MapGet("/timeseries", (
            DateOnly? from,
            DateOnly? to,
            string? bucket,
            IAuditStatsService service,
            CancellationToken ct) => service.GetTimeseriesAsync(from, to, bucket ?? "day", ct))
            .WithName("GetAuditTimeseries")
            .WithSummary("Obtiene serie temporal de auditoría")
            .WithDescription("Devuelve conteos por día para el rango indicado. Bucket soportado: 'day'.")
            .RequirePermission("Permissions.AuditTrails.View");

        return endpoints;
    }
}
