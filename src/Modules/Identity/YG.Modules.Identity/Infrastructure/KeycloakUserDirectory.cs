using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace YG.Modules.Identity.Infrastructure;

internal sealed record KeycloakOptions(
    string BaseUrl, string Realm, string AdminClientId, string AdminClientSecret);



internal sealed class KeycloakUserDirectory(IHttpClientFactory httpClientFactory, KeycloakOptions options)
    : IUserDirectory
{
    internal const string HttpClientName = "keycloak-admin";

    public async Task<string> EnsureUserAsync(
        string username, string email, string password, CancellationToken ct)
    {

        var token = await GetAdminTokenAsync(ct);

        var http = httpClientFactory.CreateClient(HttpClientName);

        http.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var create = await http.PostAsJsonAsync($"/admin/realms/{options.Realm}/users", new
        {
            username,
            email,
            enabled = true,
            emailVerified = true,
            credentials = new[] { new { type = "password", value = password, temporary = false } },
        }, ct);

        if (create.StatusCode == HttpStatusCode.Created)
            return create.Headers.Location!.Segments[^1];   // .../users/{id} — the sub

        if (create.StatusCode == HttpStatusCode.Conflict)   // already exists → fetch, don't fail
        {
            var users = await http.GetFromJsonAsync<List<KeycloakUser>>(
                $"/admin/realms/{options.Realm}/users?username={Uri.EscapeDataString(username)}&exact=true", ct);
            return users?.FirstOrDefault()?.Id
                ?? throw new InvalidOperationException(
                    $"Keycloak reported a conflict for '{username}' but lookup found nothing.");
        }

        throw new InvalidOperationException(
            $"Keycloak user creation failed: {(int)create.StatusCode} " +
            await create.Content.ReadAsStringAsync(ct));
    }

    private async Task<string> GetAdminTokenAsync(CancellationToken ct)
    {
        var http = httpClientFactory.CreateClient(HttpClientName);

        var response = await http.PostAsync(
            $"/realms/{options.Realm}/protocol/openid-connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = options.AdminClientId,
                ["client_secret"] = options.AdminClientSecret,
            }), ct);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(ct);
        return payload!.AccessToken;
    }

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken);
    private sealed record KeycloakUser(
        [property: JsonPropertyName("id")] string Id);
}