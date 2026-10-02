using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using UserService.Configuration;
using UserService.Models;

namespace UserService.Services;

public class SupabaseUserService : IUserService
{
    private readonly HttpClient _httpClient;
    private readonly SupabaseOptions _options;

    public SupabaseUserService(HttpClient httpClient, IOptions<SupabaseOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    private string BaseAdminUrl => $"{_options.Url}/auth/v1/admin/users";
    private string UserUrl(string id) => $"{_options.Url}/auth/v1/admin/users/{id}";

    public async Task<UserResponse> CrearAsync(CreateUserRequest request)
    {
        var payload = new
        {
            email = request.Correo,
            password = request.Password,
            email_confirm = true,
            user_metadata = new { nombre = request.Nombre, rol = request.Rol }
        };

        using var response = await SendAsync(HttpMethod.Post, BaseAdminUrl, payload);
        var content = await response.Content.ReadAsStringAsync();

        if (content.Contains("\"error_code\":\"email_exists\"")) throw new InvalidOperationException("Ya existe un usuario registrado con ese correo.");
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Supabase Auth respondió {(int)response.StatusCode}: {content}");

        using var json = JsonDocument.Parse(content);
        var user = MapUserElement(json.RootElement);
        if (string.IsNullOrWhiteSpace(user.Id)) throw new InvalidOperationException("Supabase no devolvió el ID del usuario.");

        return user;
    }

    public async Task<List<UserResponse>> ObtenerTodosAsync()
    {
        using var response = await SendAsync(HttpMethod.Get, BaseAdminUrl);
        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Supabase Auth respondió {(int)response.StatusCode}: {content}");

        using var doc = JsonDocument.Parse(content);
        var elements = doc.RootElement.ValueKind == JsonValueKind.Array
            ? doc.RootElement.EnumerateArray()
            : doc.RootElement.TryGetProperty("users", out var u) ? u.EnumerateArray() : default;

        var list = new List<UserResponse>();
        foreach (var el in elements) list.Add(MapUserElement(el));
        return list;
    }

    public async Task<UserResponse?> ObtenerPorIdAsync(string id)
    {
        using var response = await SendAsync(HttpMethod.Get, UserUrl(id));
        if (response.StatusCode == HttpStatusCode.NotFound) return null;

        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Supabase Auth respondió {(int)response.StatusCode}: {content}");

        using var doc = JsonDocument.Parse(content);
        return MapUserElement(doc.RootElement);
    }

    public async Task<UserResponse?> ActualizarAsync(string id, UpdateUserRequest request)
    {
        var currentUser = await ObtenerPorIdAsync(id);
        if (currentUser == null) return null;

        var payload = new Dictionary<string, object>
        {
            ["user_metadata"] = new Dictionary<string, object>
            {
                ["nombre"] = !string.IsNullOrWhiteSpace(request.Nombre) ? request.Nombre : currentUser.Nombre,
                ["rol"] = currentUser.Rol
            }
        };

        if (!string.IsNullOrWhiteSpace(request.Correo)) payload["email"] = request.Correo;
        if (!string.IsNullOrWhiteSpace(request.Correo)) payload["email_confirm"] = true;
        if (!string.IsNullOrWhiteSpace(request.Password)) payload["password"] = request.Password;

        using var response = await SendAsync(HttpMethod.Put, UserUrl(id), payload);
        var content = await response.Content.ReadAsStringAsync();

        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        if (content.Contains("\"error_code\":\"email_exists\"")) throw new InvalidOperationException("Ya existe un usuario registrado con ese correo.");
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Supabase Auth respondió {(int)response.StatusCode}: {content}");

        using var doc = JsonDocument.Parse(content);
        return MapUserElement(doc.RootElement);
    }

    public async Task<bool> EliminarAsync(string id)
    {
        using var response = await SendAsync(HttpMethod.Delete, UserUrl(id));
        if (response.StatusCode == HttpStatusCode.NotFound) return false;
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Supabase Auth respondió {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");

        return true;
    }

    public async Task<UserResponse?> CambiarRolAsync(string id, string nuevoRol)
    {
        var currentUser = await ObtenerPorIdAsync(id);
        if (currentUser == null) return null;

        var payload = new
        {
            user_metadata = new { nombre = currentUser.Nombre, rol = nuevoRol }
        };

        using var response = await SendAsync(HttpMethod.Put, UserUrl(id), payload);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;

        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Supabase Auth respondió {(int)response.StatusCode}: {content}");

        using var doc = JsonDocument.Parse(content);
        return MapUserElement(doc.RootElement);
    }

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("apikey", _options.ServiceRoleKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ServiceRoleKey);
        if (body != null) request.Content = JsonContent.Create(body);

        return _httpClient.SendAsync(request);
    }

    private static UserResponse MapUserElement(JsonElement element)
    {
        var id = element.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : "";
        var email = element.TryGetProperty("email", out var emailProp) ? emailProp.GetString() ?? "" : "";
        var meta = element.TryGetProperty("user_metadata", out var m) && m.ValueKind == JsonValueKind.Object ? m : default;

        var nombre = meta.ValueKind == JsonValueKind.Object && meta.TryGetProperty("nombre", out var n) ? n.GetString() ?? "" : "";
        var rol = meta.ValueKind == JsonValueKind.Object && meta.TryGetProperty("rol", out var r) ? r.GetString() ?? "usuario" : "usuario";

        return new UserResponse { Id = id, Correo = email, Nombre = nombre, Rol = rol };
    }
}