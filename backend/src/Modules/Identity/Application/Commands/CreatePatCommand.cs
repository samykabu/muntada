using System.Security.Cryptography;
using MediatR;
using Muntada.Identity.Domain.Events;
using Muntada.Identity.Domain.Pat;
using Muntada.Identity.Infrastructure;
using Muntada.SharedKernel.Application;

namespace Muntada.Identity.Application.Commands;

/// <summary>
/// Command to create a new Personal Access Token for API authentication.
/// </summary>
/// <param name="UserId">The unique identifier of the user creating the PAT.</param>
/// <param name="TenantId">The tenant this PAT is scoped to.</param>
/// <param name="Name">A human-readable name for the token.</param>
/// <param name="Scopes">The permission scopes to grant to this token.</param>
/// <param name="ExpiresInDays">The number of days until the token expires.</param>
public sealed record CreatePatCommand(Guid UserId, Guid TenantId, string Name, List<string> Scopes, int ExpiresInDays)
    : IRequest<CreatePatResult>;

/// <summary>
/// Result returned after a PAT is created. The plaintext token is returned only once.
/// </summary>
/// <param name="PatId">The unique identifier of the created PAT.</param>
/// <param name="PlaintextToken">The plaintext token value (returned only once; cannot be retrieved later).</param>
public sealed record CreatePatResult(Guid PatId, string PlaintextToken);

/// <summary>
/// Handles <see cref="CreatePatCommand"/> by generating a cryptographically secure token,
/// hashing it with bcrypt, creating the <see cref="PersonalAccessToken"/> entity,
/// and publishing a <see cref="PATCreatedEvent"/>.
/// </summary>
public sealed class CreatePatCommandHandler : IRequestHandler<CreatePatCommand, CreatePatResult>
{
    private readonly IdentityDbContext _dbContext;
    private readonly IIntegrationEventPublisher _eventPublisher;

    /// <summary>
    /// Initializes a new instance of <see cref="CreatePatCommandHandler"/>.
    /// </summary>
    /// <param name="dbContext">The Identity module database context.</param>
    /// <param name="eventPublisher">Publisher for integration events.</param>
    public CreatePatCommandHandler(
        IdentityDbContext dbContext,
        IIntegrationEventPublisher eventPublisher)
    {
        _dbContext = dbContext;
        _eventPublisher = eventPublisher;
    }

    /// <summary>
    /// Handles PAT creation: generates a 32-byte random token, hashes it with bcrypt,
    /// creates the entity, persists it, publishes a <see cref="PATCreatedEvent"/>,
    /// and returns the plaintext token (only returned once).
    /// </summary>
    /// <param name="request">The create PAT command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The PAT ID and plaintext token.</returns>
    public async Task<CreatePatResult> Handle(CreatePatCommand request, CancellationToken cancellationToken)
    {
        // Generate plaintext token
        var plaintextToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        // Hash with bcrypt for storage
        var tokenHash = BCrypt.Net.BCrypt.HashPassword(plaintextToken);

        // Create PAT entity
        var pat = PersonalAccessToken.Create(
            request.UserId,
            request.TenantId,
            request.Name,
            tokenHash,
            request.Scopes,
            TimeSpan.FromDays(request.ExpiresInDays));

        _dbContext.Set<PersonalAccessToken>().Add(pat);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Publish integration event
        var @event = PATCreatedEvent.Create(request.UserId, pat.Id, request.Scopes);
        await _eventPublisher.PublishAsync(@event, cancellationToken);

        return new CreatePatResult(pat.Id, plaintextToken);
    }
}
