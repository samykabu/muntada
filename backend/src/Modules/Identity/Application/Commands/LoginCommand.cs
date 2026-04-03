using MediatR;

namespace Muntada.Identity.Application.Commands;

/// <summary>
/// Command to authenticate a user with email and password credentials.
/// </summary>
/// <param name="Email">The email address to authenticate with.</param>
/// <param name="Password">The plaintext password.</param>
/// <param name="UserAgent">The User-Agent header from the client request.</param>
/// <param name="IpAddress">The IP address of the client.</param>
public sealed record LoginCommand(string Email, string Password, string UserAgent, string IpAddress)
    : IRequest<LoginResult>;

/// <summary>
/// Result returned after successful user login.
/// </summary>
/// <param name="AccessToken">The signed JWT access token.</param>
/// <param name="UserId">The unique identifier of the authenticated user.</param>
public sealed record LoginResult(string AccessToken, string UserId);
