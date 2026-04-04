using Muntada.SharedKernel.Domain.Exceptions;
using Muntada.Tenancy.Domain.Events;
using Muntada.Tenancy.Domain.Tenant;

namespace Muntada.Tenancy.Tests.Unit.Domain;

/// <summary>
/// Unit tests for the <see cref="Tenant"/> aggregate root,
/// <see cref="TenantSlug"/> value object, and <see cref="TenantBranding"/> value object.
/// </summary>
public class TenantTests
{
    private readonly Guid _createdBy = Guid.NewGuid();

    private Tenant CreateActiveTenant()
    {
        var slug = TenantSlug.Create("test-tenant");
        return Tenant.Create("Test Tenant", slug, _createdBy);
    }

    // ──────────────────────────────────────────────────────
    // Tenant.Create
    // ──────────────────────────────────────────────────────

    [Fact]
    public void Create_ShouldProduceActiveTenant_WithTrialBillingStatusAnd14DayTrial()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var tenant = CreateActiveTenant();

        // Assert
        tenant.Status.Should().Be(TenantStatus.Active);
        tenant.BillingStatus.Should().Be(BillingStatus.Trial);
        tenant.TrialEndsAt.Should().NotBeNull();
        tenant.TrialEndsAt!.Value.Should().BeCloseTo(before.AddDays(14), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_ShouldAssignCorrectNameSlugAndCreatedBy()
    {
        // Arrange
        var slug = TenantSlug.Create("my-org");

        // Act
        var tenant = Tenant.Create("My Org", slug, _createdBy);

        // Assert
        tenant.Name.Should().Be("My Org");
        tenant.Slug.Value.Should().Be("my-org");
        tenant.CreatedBy.Should().Be(_createdBy);
    }

    [Fact]
    public void Create_ShouldRaiseTenantCreatedDomainEvent()
    {
        // Act
        var tenant = CreateActiveTenant();

        // Assert
        tenant.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TenantCreatedDomainEvent>();

        var domainEvent = (TenantCreatedDomainEvent)tenant.DomainEvents[0];
        domainEvent.TenantId.Should().Be(tenant.Id);
        domainEvent.TenantName.Should().Be(tenant.Name);
        domainEvent.Slug.Should().Be(tenant.Slug.Value);
    }

    // ──────────────────────────────────────────────────────
    // Tenant.Suspend
    // ──────────────────────────────────────────────────────

    [Fact]
    public void Suspend_OnActiveTenant_ShouldTransitionToSuspended()
    {
        // Arrange
        var tenant = CreateActiveTenant();

        // Act
        tenant.Suspend();

        // Assert
        tenant.Status.Should().Be(TenantStatus.Suspended);
    }

    [Fact]
    public void Suspend_OnDeletedTenant_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var tenant = CreateActiveTenant();
        tenant.SoftDelete();

        // Act
        var act = () => tenant.Suspend();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    // ──────────────────────────────────────────────────────
    // Tenant.Activate
    // ──────────────────────────────────────────────────────

    [Fact]
    public void Activate_OnSuspendedTenant_ShouldTransitionToActive()
    {
        // Arrange
        var tenant = CreateActiveTenant();
        tenant.Suspend();

        // Act
        tenant.Activate();

        // Assert
        tenant.Status.Should().Be(TenantStatus.Active);
    }

    [Fact]
    public void Activate_OnActiveTenant_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var tenant = CreateActiveTenant();

        // Act
        var act = () => tenant.Activate();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    // ──────────────────────────────────────────────────────
    // Tenant.SoftDelete
    // ──────────────────────────────────────────────────────

    [Fact]
    public void SoftDelete_OnActiveTenant_ShouldTransitionToDeleted()
    {
        // Arrange
        var tenant = CreateActiveTenant();

        // Act
        tenant.SoftDelete();

        // Assert
        tenant.Status.Should().Be(TenantStatus.Deleted);
    }

    [Fact]
    public void SoftDelete_OnSuspendedTenant_ShouldTransitionToDeleted()
    {
        // Arrange
        var tenant = CreateActiveTenant();
        tenant.Suspend();

        // Act
        tenant.SoftDelete();

        // Assert
        tenant.Status.Should().Be(TenantStatus.Deleted);
    }

    [Fact]
    public void SoftDelete_OnDeletedTenant_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var tenant = CreateActiveTenant();
        tenant.SoftDelete();

        // Act
        var act = () => tenant.SoftDelete();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    // ──────────────────────────────────────────────────────
    // Tenant.EndTrial
    // ──────────────────────────────────────────────────────

    [Fact]
    public void EndTrial_OnTrialTenant_ShouldTransitionToActiveBillingStatus()
    {
        // Arrange
        var tenant = CreateActiveTenant();
        tenant.BillingStatus.Should().Be(BillingStatus.Trial);

        // Act
        tenant.EndTrial(BillingStatus.Active);

        // Assert
        tenant.BillingStatus.Should().Be(BillingStatus.Active);
        tenant.TrialEndsAt.Should().BeNull();
    }

    [Fact]
    public void EndTrial_OnNonTrialTenant_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var tenant = CreateActiveTenant();
        tenant.EndTrial(BillingStatus.Active);

        // Act
        var act = () => tenant.EndTrial(BillingStatus.Active);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    // ──────────────────────────────────────────────────────
    // Tenant.UpdateBranding
    // ──────────────────────────────────────────────────────

    [Fact]
    public void UpdateBranding_ShouldUpdateBranding()
    {
        // Arrange
        var tenant = CreateActiveTenant();
        var branding = TenantBranding.Create("https://logo.png", "#FF5733", "#C70039", "discuss.example.com");

        // Act
        tenant.UpdateBranding(branding);

        // Assert
        tenant.Branding.Should().Be(branding);
    }

    [Fact]
    public void UpdateBranding_OnDeletedTenant_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var tenant = CreateActiveTenant();
        tenant.SoftDelete();
        var branding = TenantBranding.Create("https://logo.png", "#FF5733", "#C70039", null);

        // Act
        var act = () => tenant.UpdateBranding(branding);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    // ──────────────────────────────────────────────────────
    // TenantSlug.Create
    // ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("my-org")]
    [InlineData("abc")]
    [InlineData("test123")]
    public void TenantSlug_Create_WithValidSlug_ShouldSucceed(string value)
    {
        // Act
        var slug = TenantSlug.Create(value);

        // Assert
        slug.Value.Should().Be(value);
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("api")]
    [InlineData("www")]
    [InlineData("support")]
    public void TenantSlug_Create_WithReservedWord_ShouldThrowValidationException(string value)
    {
        // Act
        var act = () => TenantSlug.Create(value);

        // Assert
        act.Should().Throw<ValidationException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("-invalid")]
    [InlineData("UPPER")]
    [InlineData("has spaces")]
    public void TenantSlug_Create_WithInvalidFormat_ShouldThrowValidationException(string value)
    {
        // Act
        var act = () => TenantSlug.Create(value);

        // Assert
        act.Should().Throw<ValidationException>();
    }

    [Theory]
    [InlineData("My Organization", "my-organization")]
    [InlineData("Hello World", "hello-world")]
    [InlineData("Test--Org", "test-org")]
    public void TenantSlug_GenerateFromName_ShouldConvertNameToValidSlug(string name, string expected)
    {
        // Act
        var slug = TenantSlug.GenerateFromName(name);

        // Assert
        slug.Value.Should().Be(expected);
    }

    // ──────────────────────────────────────────────────────
    // TenantBranding
    // ──────────────────────────────────────────────────────

    [Fact]
    public void TenantBranding_Create_WithValidHexColors_ShouldSucceed()
    {
        // Act
        var branding = TenantBranding.Create("https://logo.png", "#FF5733", "#C70039", "custom.example.com");

        // Assert
        branding.LogoUrl.Should().Be("https://logo.png");
        branding.PrimaryColor.Should().Be("#FF5733");
        branding.SecondaryColor.Should().Be("#C70039");
        branding.CustomDomain.Should().Be("custom.example.com");
    }

    [Theory]
    [InlineData("not-a-color")]
    [InlineData("#GGG")]
    [InlineData("FF5733")]
    [InlineData("#FF573")]
    public void TenantBranding_Create_WithInvalidHex_ShouldThrowValidationException(string invalidColor)
    {
        // Act
        var act = () => TenantBranding.Create(null, invalidColor, null, null);

        // Assert
        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void TenantBranding_Empty_ShouldReturnAllNulls()
    {
        // Act
        var branding = TenantBranding.Empty;

        // Assert
        branding.LogoUrl.Should().BeNull();
        branding.PrimaryColor.Should().BeNull();
        branding.SecondaryColor.Should().BeNull();
        branding.CustomDomain.Should().BeNull();
    }
}
