using UserService.Models;

namespace UserService.Services;

public interface IUserService
{
    Task<UserResponse> CrearAsync(CreateUserRequest request);
    Task<List<UserResponse>> ObtenerTodosAsync();
    Task<UserResponse?> ObtenerPorIdAsync(string id);
    Task<UserResponse?> ActualizarAsync(string id, UpdateUserRequest request);
    Task<bool> EliminarAsync(string id);
    Task<UserResponse?> CambiarRolAsync(string id, string nuevoRol);
}