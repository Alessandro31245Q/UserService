namespace UserService.Models;

public class UpdateUserRequest
{
    public string? Correo { get; set; }

    public string? Nombre { get; set; }

    public string? Password { get; set; }
}
