namespace Merkatto.Application.Auth;

public sealed class AuthSettings
{
    public const string SectionName = "Auth";

    public string Issuer { get; set; } = "Merkatto";
    public string Audience { get; set; } = "Merkatto";
    /// <summary>Signing key (HS256). Provided via environment/secret, never committed.</summary>
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
}
