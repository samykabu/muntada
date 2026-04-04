using MediatR;
using Microsoft.EntityFrameworkCore;
using Muntada.Tenancy.Application.Services;
using Muntada.Tenancy.Domain.Tenant;
using Muntada.Tenancy.Infrastructure;

namespace Muntada.Tenancy.Application.Commands;

/// <summary>
/// Command to update a tenant's visual branding configuration including logo,
/// color scheme, and custom domain.
/// </summary>
/// <param name="TenantId">The identifier of the tenant to update branding for.</param>
/// <param name="LogoStream">Optional logo image stream to upload.</param>
/// <param name="LogoContentType">The MIME content type of the logo (required when <paramref name="LogoStream"/> is provided).</param>
/// <param name="PrimaryColor">Optional primary brand color in hex format (#RRGGBB).</param>
/// <param name="SecondaryColor">Optional secondary brand color in hex format (#RRGGBB).</param>
/// <param name="CustomDomain">Optional custom domain for the tenant.</param>
public sealed record UpdateTenantBrandingCommand(
    Guid TenantId,
    Stream? LogoStream,
    string? LogoContentType,
    string? PrimaryColor,
    string? SecondaryColor,
    string? CustomDomain) : IRequest<UpdateTenantBrandingResult>;

/// <summary>
/// Result returned after a successful branding update.
/// </summary>
/// <param name="LogoUrl">The URL of the tenant's logo, or <c>null</c> if not set.</param>
/// <param name="PrimaryColor">The primary brand color as a hex string, or <c>null</c>.</param>
/// <param name="SecondaryColor">The secondary brand color as a hex string, or <c>null</c>.</param>
/// <param name="CustomDomain">The tenant's custom domain, or <c>null</c>.</param>
public sealed record UpdateTenantBrandingResult(
    string? LogoUrl,
    string? PrimaryColor,
    string? SecondaryColor,
    string? CustomDomain);

/// <summary>
/// Handles <see cref="UpdateTenantBrandingCommand"/> by uploading the logo (if provided),
/// validating hex colors, and updating the tenant's branding configuration.
/// </summary>
public sealed class UpdateTenantBrandingCommandHandler
    : IRequestHandler<UpdateTenantBrandingCommand, UpdateTenantBrandingResult>
{
    private readonly TenancyDbContext _dbContext;
    private readonly IBrandingService _brandingService;

    /// <summary>
    /// Initializes a new instance of <see cref="UpdateTenantBrandingCommandHandler"/>.
    /// </summary>
    /// <param name="dbContext">The Tenancy module database context.</param>
    /// <param name="brandingService">Service for processing and storing branding assets.</param>
    public UpdateTenantBrandingCommandHandler(
        TenancyDbContext dbContext,
        IBrandingService brandingService)
    {
        _dbContext = dbContext;
        _brandingService = brandingService;
    }

    /// <summary>
    /// Handles the branding update: finds the tenant, uploads logo if provided,
    /// validates colors, updates branding, and persists changes.
    /// </summary>
    /// <param name="request">The branding update command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated branding configuration.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the tenant is not found.
    /// </exception>
    public async Task<UpdateTenantBrandingResult> Handle(
        UpdateTenantBrandingCommand request,
        CancellationToken cancellationToken)
    {
        var tenant = await _dbContext.Tenants
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Tenant '{request.TenantId}' not found.");

        // Upload logo if provided
        string? logoUrl = tenant.Branding.LogoUrl;
        if (request.LogoStream is not null && request.LogoContentType is not null)
        {
            var logoResult = await _brandingService.UploadLogoAsync(
                request.TenantId,
                request.LogoStream,
                request.LogoContentType,
                cancellationToken);
            logoUrl = logoResult.LogoUrl;
        }

        // Merge with existing values — only override non-null fields
        var branding = TenantBranding.Create(
            logoUrl: logoUrl,
            primaryColor: request.PrimaryColor ?? tenant.Branding.PrimaryColor,
            secondaryColor: request.SecondaryColor ?? tenant.Branding.SecondaryColor,
            customDomain: request.CustomDomain ?? tenant.Branding.CustomDomain);

        tenant.UpdateBranding(branding);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new UpdateTenantBrandingResult(
            LogoUrl: branding.LogoUrl,
            PrimaryColor: branding.PrimaryColor,
            SecondaryColor: branding.SecondaryColor,
            CustomDomain: branding.CustomDomain);
    }
}
