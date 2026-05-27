namespace Merkatto.Domain.Common;

/// <summary>
/// Simple key-value store for runtime-configurable settings (e.g. business name).
/// Single-tenant: one row per key per installation.
/// </summary>
public sealed class AppSetting
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
