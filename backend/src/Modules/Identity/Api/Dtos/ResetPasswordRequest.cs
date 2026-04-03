namespace Muntada.Identity.Api.Dtos;

/// <summary>
/// Request DTO for resetting a password with a valid reset token.
/// </summary>
/// <param name="Token">The plaintext password reset token received via email.</param>
/// <param name="NewPassword">The new password to set.</param>
/// <param name="ConfirmNewPassword">Confirmation of the new password (must match).</param>
public sealed record ResetPasswordRequest(string Token, string NewPassword, string ConfirmNewPassword);
