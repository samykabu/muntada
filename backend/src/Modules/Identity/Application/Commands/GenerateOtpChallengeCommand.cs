using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using MediatR;
using Muntada.Identity.Application.Services;
using Muntada.Identity.Domain.Otp;
using Muntada.Identity.Infrastructure;

namespace Muntada.Identity.Application.Commands;

/// <summary>
/// Command to generate a phone OTP challenge and send the code via SMS.
/// </summary>
/// <param name="PhoneNumber">The phone number in E.164 format to send the OTP to.</param>
public sealed record GenerateOtpChallengeCommand(string PhoneNumber) : IRequest<GenerateOtpChallengeResult>;

/// <summary>
/// Result returned after an OTP challenge is created.
/// </summary>
/// <param name="ChallengeId">The unique identifier of the created OTP challenge.</param>
public sealed record GenerateOtpChallengeResult(Guid ChallengeId);

/// <summary>
/// Handles <see cref="GenerateOtpChallengeCommand"/> by validating the phone number,
/// generating a 6-digit OTP code, creating an <see cref="OtpChallenge"/>,
/// and sending the code via SMS.
/// </summary>
public sealed class GenerateOtpChallengeCommandHandler : IRequestHandler<GenerateOtpChallengeCommand, GenerateOtpChallengeResult>
{
    private static readonly TimeSpan OtpExpiry = TimeSpan.FromMinutes(5);
    private static readonly Regex E164Pattern = new(@"^\+[1-9]\d{1,14}$", RegexOptions.Compiled);

    private readonly IdentityDbContext _dbContext;
    private readonly ISmsService _smsService;

    /// <summary>
    /// Initializes a new instance of <see cref="GenerateOtpChallengeCommandHandler"/>.
    /// </summary>
    /// <param name="dbContext">The Identity module database context.</param>
    /// <param name="smsService">Service for sending SMS messages.</param>
    public GenerateOtpChallengeCommandHandler(
        IdentityDbContext dbContext,
        ISmsService smsService)
    {
        _dbContext = dbContext;
        _smsService = smsService;
    }

    /// <summary>
    /// Handles OTP challenge generation: validates the phone number format,
    /// generates a cryptographically secure 6-digit code, hashes it with SHA-256,
    /// creates the challenge entity, and sends the code via SMS.
    /// </summary>
    /// <param name="request">The generate OTP challenge command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The challenge ID for subsequent verification.</returns>
    /// <exception cref="FluentValidation.ValidationException">
    /// Thrown when the phone number is not in valid E.164 format.
    /// </exception>
    public async Task<GenerateOtpChallengeResult> Handle(GenerateOtpChallengeCommand request, CancellationToken cancellationToken)
    {
        // Validate E.164 format
        if (!E164Pattern.IsMatch(request.PhoneNumber))
        {
            throw new FluentValidation.ValidationException(
                [new FluentValidation.Results.ValidationFailure("PhoneNumber", "Phone number must be in E.164 format.")]);
        }

        // Generate 6-digit OTP code
        var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString("D6");

        // Hash code with SHA-256 for storage
        var codeHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));

        // Create OTP challenge
        var challenge = OtpChallenge.Create(request.PhoneNumber, codeHash, OtpExpiry);

        _dbContext.Set<OtpChallenge>().Add(challenge);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Send OTP via SMS
        await _smsService.SendOtpCodeAsync(request.PhoneNumber, code, cancellationToken);

        return new GenerateOtpChallengeResult(challenge.Id);
    }
}
