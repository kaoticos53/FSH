using FSH.Framework.Core.Tenancy.Dtos;
using FSH.Framework.Core.Tenancy.Features.CreateTenant;

namespace FSH.Framework.Core.Tenancy;

public interface ITenantService
{
    Task<List<TenantDetail>> GetAllAsync();
    Task<bool> ExistsWithIdAsync(string id);
    Task<bool> ExistsWithNameAsync(string name);
    Task<TenantDetail> GetByIdAsync(string id);
    Task<string> CreateAsync(CreateTenantCommand request, CancellationToken cancellationToken);
    Task<string> ActivateAsync(string id, CancellationToken cancellationToken);
    Task<string> DeactivateAsync(string id);
    Task<DateTime> UpgradeSubscription(string id, DateTime extendedExpiryDate);
}
