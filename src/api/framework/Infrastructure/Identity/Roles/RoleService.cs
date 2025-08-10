using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Core.Identity.Roles;
using FSH.Framework.Core.Identity.Roles.Features.CreateOrUpdateRole;
using FSH.Framework.Core.Identity.Roles.Features.UpdatePermissions;
using FSH.Framework.Core.Identity.Users.Abstractions;
using FSH.Framework.Infrastructure.Identity.Persistence;
using FSH.Framework.Infrastructure.Identity.RoleClaims;
using FSH.Framework.Infrastructure.Tenant;
using FSH.Starter.Shared.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FSH.Framework.Infrastructure.Identity.Roles;

/// <summary>
/// Servicio de aplicación para la gestión de roles y sus permisos.
/// Utiliza <see cref="RoleManager{TRole}"/>, <see cref="IdentityDbContext"/> y el contexto multi-tenant
/// para realizar operaciones CRUD y de autorización sobre roles.
/// </summary>
public class RoleService(RoleManager<FshRole> roleManager,
    IdentityDbContext context,
    IMultiTenantContextAccessor<FshTenantInfo> multiTenantContextAccessor,
    ICurrentUser currentUser) : IRoleService
{
    private readonly RoleManager<FshRole> _roleManager = roleManager;

    /// <summary>
    /// Obtiene la lista de roles disponibles.
    /// </summary>
    /// <returns>Lista de <see cref="RoleDto"/>.</returns>
    public async Task<IEnumerable<RoleDto>> GetRolesAsync()
    {
        return await Task.Run(() => _roleManager.Roles
            .Select(role => new RoleDto { Id = role.Id, Name = role.Name!, Description = role.Description })
            .ToList());
    }

    /// <summary>
    /// Obtiene un rol por identificador.
    /// </summary>
    /// <param name="id">Identificador del rol.</param>
    /// <returns>El rol si existe; en caso contrario, <c>null</c>.</returns>
    public async Task<RoleDto?> GetRoleAsync(string id)
    {
        FshRole? role = await _roleManager.FindByIdAsync(id);

        _ = role ?? throw new NotFoundException("role not found");

        return new RoleDto { Id = role.Id, Name = role.Name!, Description = role.Description };
    }

    /// <summary>
    /// Crea o actualiza un rol en función de su existencia.
    /// </summary>
    /// <param name="command">Comando con los datos del rol.</param>
    /// <returns>El rol creado o actualizado.</returns>
    public async Task<RoleDto> CreateOrUpdateRoleAsync(CreateOrUpdateRoleCommand command)
    {
        FshRole? role = await _roleManager.FindByIdAsync(command.Id);

        if (role != null)
        {
            role.Name = command.Name;
            role.Description = command.Description;
            await _roleManager.UpdateAsync(role);
        }
        else
        {
            role = new FshRole(command.Name, command.Description);
            await _roleManager.CreateAsync(role);
        }

        return new RoleDto { Id = role.Id, Name = role.Name!, Description = role.Description };
    }

    /// <summary>
    /// Elimina un rol por su identificador.
    /// </summary>
    /// <param name="id">Identificador del rol.</param>
    public async Task DeleteRoleAsync(string id)
    {
        FshRole? role = await _roleManager.FindByIdAsync(id);

        _ = role ?? throw new NotFoundException("role not found");

        await _roleManager.DeleteAsync(role);
    }

    /// <summary>
    /// Obtiene un rol junto con sus permisos (claims) asociados.
    /// </summary>
    /// <param name="id">Identificador del rol.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El rol con su lista de permisos.</returns>
    public async Task<RoleDto> GetWithPermissionsAsync(string id, CancellationToken cancellationToken)
    {
        var role = await GetRoleAsync(id);
        _ = role ?? throw new NotFoundException("role not found");

        role.Permissions = await context.RoleClaims
            .Where(c => c.RoleId == id && c.ClaimType == FshClaims.Permission)
            .Select(c => c.ClaimValue!)
            .ToListAsync(cancellationToken);

        return role;
    }

    /// <summary>
    /// Actualiza la lista de permisos de un rol. Filtra permisos de Root si el tenant actual no es Root.
    /// </summary>
    /// <param name="request">Comando que contiene el identificador del rol y la lista de permisos a establecer.</param>
    /// <param name="cancellationToken">Token de cancelación para abortar la operación.</param>
    /// <returns>Mensaje de confirmación.</returns>
    /// <exception cref="NotFoundException">Si el rol no existe.</exception>
    /// <exception cref="FshException">Si el rol es Admin o si alguna operación de eliminación de claim falla.</exception>
    public async Task<string> UpdatePermissionsAsync(UpdatePermissionsCommand request, CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByIdAsync(request.RoleId);
        _ = role ?? throw new NotFoundException("role not found");
        if (role.Name == FshRoles.Admin)
        {
            throw new FshException("operation not permitted");
        }

        // Respetar cancelación temprana
        cancellationToken.ThrowIfCancellationRequested();

        // Limpiar entradas vacías o de solo espacios en blanco
        request.Permissions.RemoveAll(p => string.IsNullOrWhiteSpace(p));

        if (multiTenantContextAccessor?.MultiTenantContext?.TenantInfo?.Id != TenantConstants.Root.Id)
        {
            // Remove Root Permissions if the Role is not created for Root Tenant.
            request.Permissions.RemoveAll(u => u.StartsWith("Permissions.Root.", StringComparison.InvariantCultureIgnoreCase));
        }

        // Normalizar y desduplicar permisos (recortar y Distinct case-insensitive)
        request.Permissions = request.Permissions
            .Select(p => p.Trim())
            .Where(p => p.Length > 0)
            .Distinct(StringComparer.InvariantCultureIgnoreCase)
            .ToList();

        var currentClaims = await _roleManager.GetClaimsAsync(role);

        // Remove permissions that were previously selected
        foreach (var claim in currentClaims.Where(c => !request.Permissions.Exists(p => p == c.Value)))
        {
            var result = await _roleManager.RemoveClaimAsync(role, claim);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(error => error.Description).ToList();
                throw new FshException("operation failed", errors);
            }
        }

        // Add all permissions that were not previously selected
        var addedNewClaims = false;
        foreach (string permission in request.Permissions.Where(c => !currentClaims.Any(p => p.Value == c)))
        {
            if (!string.IsNullOrWhiteSpace(permission))
            {
                context.RoleClaims.Add(new FshRoleClaim
                {
                    RoleId = role.Id,
                    ClaimType = FshClaims.Permission,
                    ClaimValue = permission,
                    CreatedBy = currentUser.GetUserId().ToString()
                });
                addedNewClaims = true;
            }
        }
        // Guardar cambios en lote si hubo inserciones
        if (addedNewClaims)
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        return "permissions updated";
    }
}
