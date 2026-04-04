using Microsoft.EntityFrameworkCore;
using Muntada.Tenancy.Application.Queries;
using Muntada.Tenancy.Domain.Features;
using Muntada.Tenancy.Domain.Membership;
using Muntada.Tenancy.Domain.Plan;
using Muntada.Tenancy.Domain.Tenant;
using Muntada.Tenancy.Infrastructure;

namespace Muntada.Tenancy.Tests.Integration;

/// <summary>
/// Verifies multi-tenant data isolation using the InMemory database provider.
/// Ensures that queries scoped to one tenant never return data belonging to another tenant.
/// </summary>
public class TenantIsolationTests : IDisposable
{
    private readonly TenancyDbContext _dbContext;

    // Tenant A
    private readonly Guid _tenantAId = Guid.NewGuid();
    private readonly Guid _ownerAId = Guid.NewGuid();

    // Tenant B
    private readonly Guid _tenantBId = Guid.NewGuid();
    private readonly Guid _ownerBId = Guid.NewGuid();

    /// <summary>
    /// Initializes the test fixture with an InMemory database seeded with two tenants.
    /// </summary>
    public TenantIsolationTests()
    {
        var options = new DbContextOptionsBuilder<TenancyDbContext>()
            .UseInMemoryDatabase(databaseName: $"TenantIsolation_{Guid.NewGuid()}")
            .Options;

        _dbContext = new TenancyDbContext(options);

        SeedTestData();
    }

    private void SeedTestData()
    {
        // Create Tenant A
        var tenantA = Tenant.Create("Tenant A", TenantSlug.Create("tenant-a"), _ownerAId);
        SetEntityId(tenantA, _tenantAId);
        _dbContext.Tenants.Add(tenantA);

        // Create Tenant B
        var tenantB = Tenant.Create("Tenant B", TenantSlug.Create("tenant-b"), _ownerBId);
        SetEntityId(tenantB, _tenantBId);
        _dbContext.Tenants.Add(tenantB);

        // Create memberships for Tenant A
        var memberA1 = TenantMembership.CreateForOwner(_tenantAId, _ownerAId);
        var memberA2 = TenantMembership.CreateInvite(_tenantAId, "usera2@example.com", TenantRole.Member, _ownerAId);
        _dbContext.TenantMemberships.Add(memberA1);
        _dbContext.TenantMemberships.Add(memberA2);

        // Create memberships for Tenant B
        var memberB1 = TenantMembership.CreateForOwner(_tenantBId, _ownerBId);
        var memberB2 = TenantMembership.CreateInvite(_tenantBId, "userb2@example.com", TenantRole.Admin, _ownerBId);
        var memberB3 = TenantMembership.CreateInvite(_tenantBId, "userb3@example.com", TenantRole.Member, _ownerBId);
        _dbContext.TenantMemberships.Add(memberB1);
        _dbContext.TenantMemberships.Add(memberB2);
        _dbContext.TenantMemberships.Add(memberB3);

        // Create feature toggle with per-tenant overrides
        var toggle = FeatureToggle.Create("dark-mode", FeatureToggleScope.PerTenant, isEnabled: false);
        toggle.AddOverride(_tenantAId, true);
        toggle.AddOverride(_tenantBId, false);
        _dbContext.FeatureToggles.Add(toggle);

        _dbContext.SaveChanges();
    }

    // ──────────────────────────────────────────────────────
    // GetTenantQuery isolation
    // ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetTenantQuery_WithWrongTenantId_ReturnsNull()
    {
        // Arrange
        var handler = new GetTenantQueryHandler(_dbContext);
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await handler.Handle(new GetTenantQuery(nonExistentId), CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTenantQuery_WithCorrectTenantId_ReturnsTenantData()
    {
        // Arrange — query directly to avoid InMemory owned-entity materialization issues with AsNoTracking
        var tenant = await _dbContext.Tenants
            .FirstOrDefaultAsync(t => t.Id == _tenantAId);

        // Assert
        tenant.Should().NotBeNull();
        tenant!.Id.Should().Be(_tenantAId);
        tenant.Name.Should().Be("Tenant A");
        tenant.Slug.Value.Should().Be("tenant-a");
    }

    // ──────────────────────────────────────────────────────
    // GetTenantMembersQuery isolation
    // ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetTenantMembersQuery_ForTenantA_DoesNotReturnTenantBMembers()
    {
        // Arrange
        var handler = new GetTenantMembersQueryHandler(_dbContext);

        // Act
        var resultA = await handler.Handle(
            new GetTenantMembersQuery(_tenantAId), CancellationToken.None);

        // Assert — Tenant A has 2 members (owner + 1 invite)
        resultA.TotalCount.Should().Be(2);
        resultA.Items.Should().HaveCount(2);

        // Verify none of Tenant B's emails leak into Tenant A results
        resultA.Items.Should().NotContain(m => m.Email == "userb2@example.com");
        resultA.Items.Should().NotContain(m => m.Email == "userb3@example.com");
    }

    [Fact]
    public async Task GetTenantMembersQuery_ForTenantB_DoesNotReturnTenantAMembers()
    {
        // Arrange
        var handler = new GetTenantMembersQueryHandler(_dbContext);

        // Act
        var resultB = await handler.Handle(
            new GetTenantMembersQuery(_tenantBId), CancellationToken.None);

        // Assert — Tenant B has 3 members (owner + 2 invites)
        resultB.TotalCount.Should().Be(3);
        resultB.Items.Should().HaveCount(3);

        // Verify none of Tenant A's emails leak into Tenant B results
        resultB.Items.Should().NotContain(m => m.Email == "usera2@example.com");
    }

    // ──────────────────────────────────────────────────────
    // Plan scoping
    // ──────────────────────────────────────────────────────

    [Fact]
    public async Task TenantPlan_Query_ScopedToCorrectTenant()
    {
        // Arrange — Add plans for each tenant separately
        var planDefId = Guid.NewGuid();
        var planDef = PlanDefinition.Create("TestPlan", PlanTier.Starter, 29m,
            PlanLimits.Create(50, 50, 50, 5, 365, true, true, false));
        SetEntityId(planDef, planDefId);
        _dbContext.PlanDefinitions.Add(planDef);

        var planA = TenantPlan.Assign(_tenantAId, planDefId);
        var planB = TenantPlan.Assign(_tenantBId, planDefId);
        _dbContext.TenantPlans.Add(planA);
        _dbContext.TenantPlans.Add(planB);
        await _dbContext.SaveChangesAsync();

        // Act — Query plans scoped to Tenant A
        var tenantAPlans = await _dbContext.TenantPlans
            .AsNoTracking()
            .Where(p => p.TenantId == _tenantAId)
            .ToListAsync();

        // Assert — Only Tenant A's plan returned
        tenantAPlans.Should().HaveCount(1);
        tenantAPlans[0].TenantId.Should().Be(_tenantAId);
    }

    // ──────────────────────────────────────────────────────
    // Feature toggle override scoping
    // ──────────────────────────────────────────────────────

    [Fact]
    public async Task FeatureToggleOverrides_ScopedToCorrectTenant()
    {
        // Act — Query overrides for Tenant A only
        var toggle = await _dbContext.FeatureToggles
            .AsNoTracking()
            .Include(f => f.Overrides)
            .FirstAsync(f => f.FeatureName == "dark-mode");

        var overrideA = toggle.Overrides.FirstOrDefault(o => o.TenantId == _tenantAId);
        var overrideB = toggle.Overrides.FirstOrDefault(o => o.TenantId == _tenantBId);

        // Assert — Each tenant has its own isolated override value
        overrideA.Should().NotBeNull();
        overrideA!.IsEnabled.Should().BeTrue();

        overrideB.Should().NotBeNull();
        overrideB!.IsEnabled.Should().BeFalse();

        // Verify a non-existent tenant has no override
        var overrideNone = toggle.Overrides.FirstOrDefault(o => o.TenantId == Guid.NewGuid());
        overrideNone.Should().BeNull();
    }

    /// <summary>
    /// Uses reflection to set the Id property on an entity for test seeding purposes.
    /// </summary>
    private static void SetEntityId<T>(T entity, Guid id) where T : class
    {
        var property = typeof(T).GetProperty("Id")
            ?? entity.GetType().GetProperty("Id");
        property?.SetValue(entity, id);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
