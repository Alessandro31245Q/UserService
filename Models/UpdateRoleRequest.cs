using System.Text.Json.Serialization;

namespace UserService.Models;

public class UpdateRoleRequest
{
    private string _rol = string.Empty;

    public string Rol
    {
        get => _rol;
        set => _rol = value;
    }

    [JsonPropertyName("role")]
    public string? RoleAlias
    {
        get => _rol;
        set
        {
            if (!string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(_rol))
            {
                _rol = value;
            }
        }
    }
}
