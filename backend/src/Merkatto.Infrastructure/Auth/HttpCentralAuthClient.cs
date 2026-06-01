using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Merkatto.Application.Auth;
using Merkatto.Domain.Auth;
using Microsoft.Extensions.Logging;

namespace Merkatto.Infrastructure.Auth;

/// <summary>
/// HTTP client for the central identity server. Registered only in a bodega desktop host that has
/// a central configured. Reaches the central's own auth endpoints (the same API that serves the
/// cloud), so no extra server-side surface is needed.
/// </summary>
public sealed class HttpCentralAuthClient(HttpClient http, ILogger<HttpCentralAuthClient> logger)
    : ICentralAuthClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<CentralLoginResult?> ValidateAsync(string email, string password, CancellationToken ct)
    {
        HttpResponseMessage response;
        try
        {
            response = await http.PostAsJsonAsync(
                "api/v1/auth/login", new { email, password }, Json, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // Network failure or timeout: treat as offline so the caller can use the local cache.
            logger.LogWarning(ex, "Central auth unreachable; falling back to local cache.");
            return null;
        }

        if (response.StatusCode is HttpStatusCode.Unauthorized)
            throw new CentralRejectedException("Credenciales inválidas.");

        if (!response.IsSuccessStatusCode)
        {
            // Transient server-side problem: don't lock the bodega out — fall back to cache.
            logger.LogWarning("Central auth returned {Status}; falling back to local cache.", (int)response.StatusCode);
            return null;
        }

        var body = await response.Content.ReadFromJsonAsync<CentralAuthResponse>(Json, ct);
        if (body?.User is null) return null;

        var u = body.User;
        return new CentralLoginResult(
            Email: u.Email.Trim().ToLowerInvariant(),
            FullName: u.FullName,
            Role: (Role)u.Role,
            MustChangePassword: u.MustChangePassword,
            IsActive: true,
            BusinessName: u.BusinessName);
    }

    public async Task ChangePasswordAsync(string email, string currentPassword, string newPassword, CancellationToken ct)
    {
        // The central's change-password endpoint requires authentication, so log in first to get a
        // token (login succeeds even when must_change_password is set), then change the password.
        HttpResponseMessage loginResponse;
        try
        {
            loginResponse = await http.PostAsJsonAsync(
                "api/v1/auth/login", new { email, password = currentPassword }, Json, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new CentralUnavailableException("No se pudo contactar al servidor central.");
        }

        if (loginResponse.StatusCode is HttpStatusCode.Unauthorized)
            throw new CentralRejectedException("La contraseña actual es incorrecta.");
        if (!loginResponse.IsSuccessStatusCode)
            throw new CentralUnavailableException("El servidor central no está disponible.");

        var auth = await loginResponse.Content.ReadFromJsonAsync<CentralAuthResponse>(Json, ct);
        if (string.IsNullOrEmpty(auth?.AccessToken))
            throw new CentralUnavailableException("Respuesta inválida del servidor central.");

        using var changeRequest = new HttpRequestMessage(HttpMethod.Put, "api/v1/auth/change-password")
        {
            Content = JsonContent.Create(new { currentPassword, newPassword }, options: Json)
        };
        changeRequest.Headers.Authorization = new("Bearer", auth.AccessToken);

        HttpResponseMessage changeResponse;
        try
        {
            changeResponse = await http.SendAsync(changeRequest, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new CentralUnavailableException("No se pudo contactar al servidor central.");
        }

        if (changeResponse.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest
            or HttpStatusCode.UnprocessableEntity)
        {
            var detail = await SafeReadDetailAsync(changeResponse, ct);
            throw new CentralRejectedException(detail ?? "No se pudo cambiar la contraseña.");
        }
        if (!changeResponse.IsSuccessStatusCode)
            throw new CentralUnavailableException("El servidor central no está disponible.");
    }

    private static async Task<string?> SafeReadDetailAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsLite>(Json, ct);
            return problem?.Detail;
        }
        catch
        {
            return null;
        }
    }

    private sealed record CentralAuthResponse(string? AccessToken, CentralUser? User);

    private sealed record CentralUser(
        string Email, string FullName, int Role, bool MustChangePassword, string? BusinessName);

    private sealed record ProblemDetailsLite(string? Detail);
}
