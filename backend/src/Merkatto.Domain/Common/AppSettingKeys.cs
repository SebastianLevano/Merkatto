namespace Merkatto.Domain.Common;

public static class AppSettingKeys
{
    public const string BusinessName = "business_name";

    /// <summary>Email of the Encargado this desktop install is bound to. Set on the first
    /// successful online login; later logins by a different account are rejected so a bodega's
    /// local data is never exposed to another user.</summary>
    public const string BoundUserEmail = "bound_user_email";
}
