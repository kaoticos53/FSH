namespace FSH.Framework.Core.Tenancy.Dtos;

public class TenantDetail
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? SubscriptionExpiryDate { get; set; }
}
