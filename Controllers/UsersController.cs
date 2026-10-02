using Microsoft.AspNetCore.Mvc;
using UserService.Models;
using UserService.Services;

namespace UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost]
    public async Task<IActionResult> Crear(CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Correo)) return BadRequest("El correo es obligatorio.");
        if (string.IsNullOrWhiteSpace(request.Password)) return BadRequest("La contraseña es obligatoria.");
        if (request.Password.Length < 6) return BadRequest("La contraseña debe tener al menos 6 caracteres.");
        if (string.IsNullOrWhiteSpace(request.Nombre)) return BadRequest("El nombre es obligatorio.");
        if (request.Rol is not ("admin" or "usuario")) return BadRequest("El rol debe ser 'admin' o 'usuario'.");

        var usuario = await _userService.CrearAsync(request);
        return CreatedAtAction(nameof(Obtener), new { id = usuario.Id }, usuario);
    }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var usuarios = await _userService.ObtenerTodosAsync();
        return Ok(usuarios);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Obtener(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return BadRequest("El id es obligatorio.");

        var usuario = await _userService.ObtenerPorIdAsync(id);
        if (usuario == null) return NotFound($"No se encontró el usuario con ID '{id}'.");

        return Ok(usuario);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Actualizar(string id, UpdateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(id)) return BadRequest("El id es obligatorio.");
        if (string.IsNullOrWhiteSpace(request.Correo) && string.IsNullOrWhiteSpace(request.Nombre) && string.IsNullOrWhiteSpace(request.Password)) return BadRequest("Debe proporcionar al menos un campo para actualizar (nombre, correo o contraseña).");
        if (!string.IsNullOrWhiteSpace(request.Password) && request.Password.Length < 6) return BadRequest("La contraseña debe tener al menos 6 caracteres.");

        var usuario = await _userService.ActualizarAsync(id, request);
        if (usuario == null) return NotFound($"No se encontró el usuario con ID '{id}'.");

        return Ok(usuario);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Eliminar(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return BadRequest("El id es obligatorio.");

        var eliminado = await _userService.EliminarAsync(id);
        if (!eliminado) return NotFound($"No se encontró el usuario con ID '{id}'.");

        return NoContent();
    }

    [HttpPatch("{id}/role")]
    [HttpPatch("{id}/rol")]
    public async Task<IActionResult> CambiarRol(string id, UpdateRoleRequest request)
    {
        if (string.IsNullOrWhiteSpace(id)) return BadRequest("El id es obligatorio.");
        if (string.IsNullOrWhiteSpace(request.Rol)) return BadRequest("El rol es obligatorio.");
        if (request.Rol is not ("admin" or "usuario")) return BadRequest("El rol debe ser 'admin' o 'usuario'.");

        var usuario = await _userService.CambiarRolAsync(id, request.Rol);
        if (usuario == null) return NotFound($"No se encontró el usuario con ID '{id}'.");

        return Ok(usuario);
    }
}