using System.ComponentModel.DataAnnotations;

namespace FSH.Framework.Core.Tenancy.Features.CreateTenant;

public class CreateTenantCommand
{
    [Required]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public string ConnectionString { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = true;
    public DateTime? SubscriptionExpiryDate { get; set; }
}
