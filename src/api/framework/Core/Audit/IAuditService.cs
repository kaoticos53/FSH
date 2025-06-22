namespace FSH.Framework.Core.Audit;

/// <summary>
/// Defines the contract for a service that handles audit trail operations.
/// This service is responsible for retrieving and managing audit trail entries.
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Retrieves all audit trail entries for a specific user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose audit trails are being requested.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see cref="AuditTrail"/> entries.</returns>
    Task<List<AuditTrail>> GetUserTrailsAsync(Guid userId);
}
