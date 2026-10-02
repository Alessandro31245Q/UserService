namespace UserService.Models;

public class CreateUserRequest
{
    public string Correo { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string Rol { get; set; } = "usuario";
}