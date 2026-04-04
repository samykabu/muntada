using System.Security.Claims;

namespace Muntada.Rooms.Api.Extensions;

/// <summary>
/// Extension methods for <see cref="ClaimsPrincipal"/> to extract user identity.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Gets the current user ID from the claims principal.
    /// Falls back to "anonymous" if no identity claim is found.
    /// </summary>
    /// <param name="user">The claims principal.</param>
    /// <returns>The user identifier.</returns>
    public static string GetCurrentUserId(this ClaimsPrincipal user)
    {
        return user?.FindFirst("sub")?.Value
            ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? "anonymous";
    }
}
