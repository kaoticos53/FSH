using FSH.Framework.Core.Audit;
using FSH.Framework.Infrastructure.Identity.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FSH.Framework.Infrastructure.Identity.Audit;

/// <summary>
/// Implementación del servicio de estadísticas de auditoría sobre <see cref="IdentityDbContext"/>.
/// </summary>
public class AuditStatsService(IdentityDbContext context) : IAuditStatsService
{
    public async Task<AuditStatsSummaryDto> GetSummaryAsync(DateOnly? from, DateOnly? to, int topN = 5, CancellationToken ct = default)
    {
        var (fromDt, toDt) = NormalizeRange(from, to);
        var query = context.AuditTrails.AsNoTracking()
            .Where(a => a.DateTime >= fromDt && a.DateTime <= toDt);

        var totalTask = query.CountAsync(ct);
        var byOpTask = query
            .GroupBy(a => a.Operation ?? "Unknown")
            .Select(g => new { Operation = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Operation, x => x.Count, ct);
        var topEntitiesTask = query
            .GroupBy(a => a.Entity ?? "Unknown")
            .Select(g => new EntityCountDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Entity)
            .Take(topN)
            .ToListAsync(ct);
        var topUsersTask = query
            .GroupBy(a => a.UserId)
            .Select(g => new UserCountDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.UserId)
            .Take(topN)
            .ToListAsync(ct);

        await Task.WhenAll(totalTask, byOpTask, topEntitiesTask, topUsersTask);
        return new AuditStatsSummaryDto(
            Total: totalTask.Result,
            ByOperation: byOpTask.Result,
            TopEntities: topEntitiesTask.Result,
            TopUsers: topUsersTask.Result
        );
    }

    public async Task<List<AuditTimeseriesPointDto>> GetTimeseriesAsync(DateOnly? from, DateOnly? to, string bucket = "day", CancellationToken ct = default)
    {
        if (!string.Equals(bucket, "day", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentOutOfRangeException(nameof(bucket), "Solo se admite el bucket 'day'.");

        var (fromDt, toDt) = NormalizeRange(from, to);

        var points = await context.AuditTrails.AsNoTracking()
            .Where(a => a.DateTime >= fromDt && a.DateTime <= toDt)
            .GroupBy(a => DateOnly.FromDateTime(a.DateTime.DateTime))
            .Select(g => new AuditTimeseriesPointDto(g.Key, g.Count()))
            .OrderBy(p => p.Date)
            .ToListAsync(ct);

        // Asegurar continuidad del rango con ceros donde no hay datos.
        var result = new List<AuditTimeseriesPointDto>();
        for (var d = DateOnly.FromDateTime(fromDt.DateTime.Date);
             d <= DateOnly.FromDateTime(toDt.DateTime.Date);
             d = d.AddDays(1))
        {
            var existing = points.FirstOrDefault(p => p.Date == d);
            result.Add(existing is { } p ? p : new AuditTimeseriesPointDto(d, 0));
        }
        return result;
    }

    private static (DateTimeOffset from, DateTimeOffset to) NormalizeRange(DateOnly? from, DateOnly? to)
    {
        var toDate = to ?? DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var fromDate = from ?? toDate.AddDays(-30);
        var fromDt = new DateTimeOffset(fromDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var toDt = new DateTimeOffset(toDate.ToDateTime(new TimeOnly(23, 59, 59, 999)), TimeSpan.Zero);
        return (fromDt, toDt);
    }
}
