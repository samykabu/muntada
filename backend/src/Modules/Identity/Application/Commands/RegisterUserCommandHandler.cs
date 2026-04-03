using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.Identity.Application.Services;
using Muntada.Identity.Domain.EmailVerification;
using Muntada.Identity.Domain.Events;
using Muntada.Identity.Domain.User;
using Muntada.Identity.Infrastructure;
using Muntada.SharedKernel.Application;

namespace Muntada.Identity.Application.Commands;

/// <summary>
/// Handles <see cref="RegisterUserCommand"/> by creating a new user,
/// generating an email verification token, and publishing a registration event.
/// </summary>
public sealed class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, RegisterUserResult>
{
    private readonly IdentityDbContext _dbContext;
    private readonly IEmailService _emailService;
    private readonly IIntegrationEventPublisher _eventPublisher;

    /// <summary>
    /// Initializes a new instance of <see cref="RegisterUserCommandHandler"/>.
    /// </summary>
    /// <param name="dbContext">The Identity module database context.</param>
    /// <param name="emailService">Service for sending verification emails.</param>
    /// <param name="eventPublisher">Publisher for integration events.</param>
    public RegisterUserCommandHandler(
        IdentityDbContext dbContext,
        IEmailService emailService,
        IIntegrationEventPublisher eventPublisher)
    {
        _dbContext = dbContext;
        _emailService = emailService;
        _eventPublisher = eventPublisher;
    }

    /// <summary>
    /// Handles user registration: validates uniqueness, creates user and verification token,
    /// sends verification email, and publishes the <see cref="UserRegisteredEvent"/>.
    /// </summary>
    /// <param name="request">The registration command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The registration result containing the new user's ID and email.</returns>
    /// <exception cref="FluentValidation.ValidationException">
    /// Thrown with a generic message when registration fails (e.g., duplicate email) per FR-018.
    /// </exception>
    public async Task<RegisterUserResult> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        // Check if email already exists — generic error to prevent email enumeration (FR-018)
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var emailExists = await _dbContext.Set<User>()
            .AnyAsync(u => u.Email.Value == normalizedEmail, cancellationToken);

        if (emailExists)
        {
            throw new FluentValidation.ValidationException(
                [new FluentValidation.Results.ValidationFailure("", "Registration failed.")]);
        }

        // Create domain objects
        var email = Email.Create(request.Email);
        var passwordHash = PasswordHash.Create(request.Password);
        var user = User.Create(email, passwordHash, "system");

        // Generate verification token
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
        var verificationToken = EmailVerificationToken.Create(
            user.Id,
            tokenHash,
            TimeSpan.FromHours(24));

        // Persist user and token in a single transaction
        _dbContext.Set<User>().Add(user);
        _dbContext.Set<EmailVerificationToken>().Add(verificationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Send verification email with the raw (unhashed) token
        await _emailService.SendVerificationEmailAsync(email.Value, rawToken, cancellationToken);

        // Publish integration event
        var @event = UserRegisteredEvent.Create(user.Id, email.Value);
        await _eventPublisher.PublishAsync(@event, cancellationToken);

        return new RegisterUserResult(user.Id.ToString(), email.Value);
    }
}
