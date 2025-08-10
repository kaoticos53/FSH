using FSH.Framework.Core.Identity.Roles.Features.CreateOrUpdateRole;
using FSH.Framework.Core.Identity.Roles.Features.UpdatePermissions;

namespace FSH.Framework.Core.Identity.Roles;

/// <summary>
/// Servicio de dominio para la gestión de roles y sus permisos.
/// Proporciona operaciones para consultar, crear/actualizar, eliminar y administrar permisos.
/// </summary>
public interface IRoleService
{
    /// <summary>
    /// Obtiene todos los roles disponibles.
    /// </summary>
    /// <returns>Lista de roles.</returns>
    Task<IEnumerable<RoleDto>> GetRolesAsync();

    /// <summary>
    /// Obtiene un rol por su identificador.
    /// </summary>
    /// <param name="id">Identificador del rol.</param>
    /// <returns>El rol encontrado o <c>null</c> si no existe.</returns>
    Task<RoleDto?> GetRoleAsync(string id);

    /// <summary>
    /// Crea o actualiza un rol según exista o no.
    /// </summary>
    /// <param name="command">Comando con los datos del rol.</param>
    /// <returns>Rol creado o actualizado.</returns>
    Task<RoleDto> CreateOrUpdateRoleAsync(CreateOrUpdateRoleCommand command);

    /// <summary>
    /// Elimina un rol por su identificador.
    /// </summary>
    /// <param name="id">Identificador del rol.</param>
    Task DeleteRoleAsync(string id);

    /// <summary>
    /// Obtiene un rol con la lista de permisos asociados.
    /// </summary>
    /// <param name="id">Identificador del rol.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El rol con sus permisos.</returns>
    Task<RoleDto> GetWithPermissionsAsync(string id, CancellationToken cancellationToken);
    
    /// <summary>
    /// Actualiza los permisos asignados a un rol.
    /// </summary>
    /// <param name="request">Comando con la lista de permisos a establecer.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Mensaje de confirmación de la operación.</returns>
    Task<string> UpdatePermissionsAsync(UpdatePermissionsCommand request, CancellationToken cancellationToken = default);
}
