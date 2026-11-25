using UserManagementAPI.DTOs;
using UserManagementAPI.Models;

namespace UserManagementAPI.Services;

/// <summary>
/// Contrato de acceso a datos para la gestión de usuarios.
/// </summary>
public interface IUserRepository
{
    PagedResult<User> GetAll(string? department = null, bool? isActive = null, int page = 1, int pageSize = 20);
    User? GetById(int id);
    bool EmailExists(string email, int? excludeUserId = null);
    User Create(User user);
    User? Update(int id, User updatedUser);
    bool Delete(int id);
}
