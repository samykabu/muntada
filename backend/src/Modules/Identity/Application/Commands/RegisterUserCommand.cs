using MediatR;

namespace Muntada.Identity.Application.Commands;

/// <summary>
/// Command to register a new user with email and password credentials.
/// </summary>
/// <param name="Email">The email address to register with.</param>
/// <param name="Password">The plaintext password (min 12 chars, uppercase, digit, special).</param>
/// <param name="ConfirmPassword">Must match <paramref name="Password"/>.</param>
public sealed record RegisterUserCommand(string Email, string Password, string ConfirmPassword)
    : IRequest<RegisterUserResult>;

/// <summary>
/// Result returned after successful user registration.
/// </summary>
/// <param name="UserId">The unique identifier of the newly created user.</param>
/// <param name="Email">The normalized email address of the registered user.</param>
public sealed record RegisterUserResult(string UserId, string Email);
