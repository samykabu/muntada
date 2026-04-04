using Microsoft.EntityFrameworkCore;
using Muntada.Tenancy.Domain.Features;
using Muntada.Tenancy.Domain.Membership;
using Muntada.Tenancy.Domain.Plan;
using Muntada.Tenancy.Domain.Retention;
using Muntada.Tenancy.Domain.Tenant;
using Muntada.Tenancy.Domain.Usage;

namespace Muntada.Tenancy.Infrastructure;

/// <summary>
/// Entity Framework Core DbContext for the Tenancy module.
/// All tables are created under the <c>[tenancy]</c> SQL Server schema
/// per Constitution I (Modular Monolith Discipline).
/// </summary>
public class TenancyDbContext : DbContext
{
    /// <summary>The SQL Server schema name for tenancy module tables.</summary>
    public const string SchemaName = "tenancy";

    /// <summary>Gets or sets the Tenants DbSet.</summary>
    public DbSet<Domain.Tenant.Tenant> Tenants => Set<Domain.Tenant.Tenant>();

    /// <summary>Gets or sets the TenantMemberships DbSet.</summary>
    public DbSet<TenantMembership> TenantMemberships => Set<TenantMembership>();

    /// <summary>Gets or sets the TenantInviteTokens DbSet.</summary>
    public DbSet<TenantInviteToken> TenantInviteTokens => Set<TenantInviteToken>();

    /// <summary>Gets or sets the PlanDefinitions DbSet.</summary>
    public DbSet<PlanDefinition> PlanDefinitions => Set<PlanDefinition>();

    /// <summary>Gets or sets the TenantPlans DbSet.</summary>
    public DbSet<TenantPlan> TenantPlans => Set<TenantPlan>();

    /// <summary>Gets or sets the RetentionPolicies DbSet.</summary>
    public DbSet<Domain.Retention.RetentionPolicy> RetentionPolicies => Set<Domain.Retention.RetentionPolicy>();

    /// <summary>Gets or sets the FeatureToggles DbSet.</summary>
    public DbSet<FeatureToggle> FeatureToggles => Set<FeatureToggle>();

    /// <summary>Gets or sets the FeatureToggleOverrides DbSet.</summary>
    public DbSet<FeatureToggleOverride> FeatureToggleOverrides => Set<FeatureToggleOverride>();

    /// <summary>Gets or sets the TenantUsageSnapshots DbSet.</summary>
    public DbSet<TenantUsageSnapshot> TenantUsageSnapshots => Set<TenantUsageSnapshot>();

    /// <summary>
    /// Initializes a new instance of the <see cref="TenancyDbContext"/> class.
    /// </summary>
    public TenancyDbContext(DbContextOptions<TenancyDbContext> options)
        : base(options)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);

        ConfigureTenant(modelBuilder);
        ConfigureTenantMembership(modelBuilder);
        ConfigureTenantInviteToken(modelBuilder);
        ConfigurePlanDefinition(modelBuilder);
        ConfigureTenantPlan(modelBuilder);
        ConfigureRetentionPolicy(modelBuilder);
        ConfigureFeatureToggle(modelBuilder);
        ConfigureTenantUsageSnapshot(modelBuilder);
        SeedPlanDefinitions(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    private static void ConfigureTenant(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Domain.Tenant.Tenant>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Name).IsRequired().HasMaxLength(100);
            entity.Property(t => t.Status).IsRequired().HasConversion<string>();
            entity.Property(t => t.BillingStatus).IsRequired().HasConversion<string>();
            entity.Property(t => t.CreatedBy).IsRequired();
            entity.Property(t => t.Version).IsConcurrencyToken();

            entity.OwnsOne(t => t.Slug, slug =>
            {
                slug.Property(s => s.Value).HasColumnName("Slug").IsRequired().HasMaxLength(63);
                slug.HasIndex(s => s.Value).IsUnique();
            });

            entity.OwnsOne(t => t.Branding, branding =>
            {
                branding.Property(b => b.LogoUrl).HasColumnName("LogoUrl").HasMaxLength(500);
                branding.Property(b => b.PrimaryColor).HasColumnName("PrimaryColor").HasMaxLength(7);
                branding.Property(b => b.SecondaryColor).HasColumnName("SecondaryColor").HasMaxLength(7);
                branding.Property(b => b.CustomDomain).HasColumnName("CustomDomain").HasMaxLength(255);
            });
        });
    }

    private static void ConfigureTenantMembership(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenantMembership>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.TenantId).IsRequired();
            entity.Property(m => m.InvitedEmail).HasMaxLength(256);
            entity.Property(m => m.Role).IsRequired().HasConversion<string>();
            entity.Property(m => m.Status).IsRequired().HasConversion<string>();

            entity.HasIndex(m => new { m.TenantId, m.UserId })
                .IsUnique()
                .HasFilter("[Status] <> 'Inactive' AND [UserId] IS NOT NULL");

            entity.HasIndex(m => new { m.TenantId, m.InvitedEmail })
                .IsUnique()
                .HasFilter("[Status] = 'Pending' AND [InvitedEmail] IS NOT NULL");

            entity.HasOne<Domain.Tenant.Tenant>()
                .WithMany()
                .HasForeignKey(m => m.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureTenantInviteToken(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenantInviteToken>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Token).IsRequired().HasMaxLength(64);
            entity.Property(t => t.MembershipId).IsRequired();

            entity.HasIndex(t => t.Token).IsUnique();

            entity.HasOne<TenantMembership>()
                .WithMany()
                .HasForeignKey(t => t.MembershipId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigurePlanDefinition(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlanDefinition>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(50);
            entity.Property(p => p.Tier).IsRequired().HasConversion<string>();
            entity.Property(p => p.MonthlyPriceUsd).HasPrecision(10, 2);

            entity.HasIndex(p => p.Name).IsUnique();

            entity.OwnsOne(p => p.Limits, limits =>
            {
                limits.Property(l => l.MaxRoomsPerMonth).HasColumnName("MaxRoomsPerMonth");
                limits.Property(l => l.MaxParticipantsPerRoom).HasColumnName("MaxParticipantsPerRoom");
                limits.Property(l => l.MaxStorageGB).HasColumnName("MaxStorageGB");
                limits.Property(l => l.MaxRecordingHoursPerMonth).HasColumnName("MaxRecordingHoursPerMonth");
                limits.Property(l => l.MaxDataRetentionDays).HasColumnName("MaxDataRetentionDays");
                limits.Property(l => l.AllowRecording).HasColumnName("AllowRecording");
                limits.Property(l => l.AllowGuestAccess).HasColumnName("AllowGuestAccess");
                limits.Property(l => l.AllowCustomBranding).HasColumnName("AllowCustomBranding");
            });
        });
    }

    private static void ConfigureTenantPlan(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenantPlan>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.TenantId).IsRequired();
            entity.Property(p => p.PlanDefinitionId).IsRequired();

            entity.HasIndex(p => new { p.TenantId, p.IsCurrent })
                .IsUnique()
                .HasFilter("[IsCurrent] = 1");

            entity.HasOne<Domain.Tenant.Tenant>()
                .WithMany()
                .HasForeignKey(p => p.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<PlanDefinition>()
                .WithMany()
                .HasForeignKey(p => p.PlanDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureRetentionPolicy(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Domain.Retention.RetentionPolicy>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.TenantId).IsRequired();

            entity.HasIndex(r => r.TenantId).IsUnique();

            entity.HasOne<Domain.Tenant.Tenant>()
                .WithMany()
                .HasForeignKey(r => r.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureFeatureToggle(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FeatureToggle>(entity =>
        {
            entity.HasKey(f => f.Id);
            entity.Property(f => f.FeatureName).IsRequired().HasMaxLength(100);
            entity.Property(f => f.Scope).IsRequired().HasConversion<string>();
            entity.Property(f => f.Version).IsConcurrencyToken();

            entity.HasIndex(f => f.FeatureName).IsUnique();

            entity.HasMany(f => f.Overrides)
                .WithOne()
                .HasForeignKey(o => o.FeatureToggleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(f => f.Overrides).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<FeatureToggleOverride>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.TenantId).IsRequired();

            entity.HasIndex(o => new { o.FeatureToggleId, o.TenantId }).IsUnique();
        });
    }

    private static void ConfigureTenantUsageSnapshot(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenantUsageSnapshot>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.TenantId).IsRequired();
            entity.Property(s => s.SnapshotDate).IsRequired();
            entity.Property(s => s.StorageUsedGB).HasPrecision(10, 4);
            entity.Property(s => s.RecordingHoursUsed).HasPrecision(10, 2);

            entity.HasIndex(s => new { s.TenantId, s.SnapshotDate }).IsUnique();

            entity.HasOne<Domain.Tenant.Tenant>()
                .WithMany()
                .HasForeignKey(s => s.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void SeedPlanDefinitions(ModelBuilder modelBuilder)
    {
        var freeLimits = PlanLimits.Create(5, 10, 1, 0, 90, false, false, false);
        var trialLimits = PlanLimits.Create(100, 100, 10, 10, 365, true, true, true);
        var starterLimits = PlanLimits.Create(50, 50, 50, 5, 365, true, true, false);
        var proLimits = PlanLimits.Create(200, 100, 200, 50, 365, true, true, true);
        var enterpriseLimits = PlanLimits.Create(0, 500, 1000, 200, 3650, true, true, true);

        modelBuilder.Entity<PlanDefinition>(entity =>
        {
            entity.HasData(
                CreatePlanSeed("00000000-0000-0000-0000-000000000001", "Free", PlanTier.Free, 0m),
                CreatePlanSeed("00000000-0000-0000-0000-000000000002", "Trial", PlanTier.Trial, 0m),
                CreatePlanSeed("00000000-0000-0000-0000-000000000003", "Starter", PlanTier.Starter, 29m),
                CreatePlanSeed("00000000-0000-0000-0000-000000000004", "Professional", PlanTier.Professional, 99m),
                CreatePlanSeed("00000000-0000-0000-0000-000000000005", "Enterprise", PlanTier.Enterprise, 0m)
            );

            entity.OwnsOne(p => p.Limits).HasData(
                CreateLimitsSeed("00000000-0000-0000-0000-000000000001", freeLimits),
                CreateLimitsSeed("00000000-0000-0000-0000-000000000002", trialLimits),
                CreateLimitsSeed("00000000-0000-0000-0000-000000000003", starterLimits),
                CreateLimitsSeed("00000000-0000-0000-0000-000000000004", proLimits),
                CreateLimitsSeed("00000000-0000-0000-0000-000000000005", enterpriseLimits)
            );
        });
    }

    private static object CreatePlanSeed(string id, string name, PlanTier tier, decimal price)
    {
        return new
        {
            Id = Guid.Parse(id),
            Name = name,
            Tier = tier,
            MonthlyPriceUsd = price,
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
    }

    private static object CreateLimitsSeed(string planId, PlanLimits limits)
    {
        return new
        {
            PlanDefinitionId = Guid.Parse(planId),
            limits.MaxRoomsPerMonth,
            limits.MaxParticipantsPerRoom,
            limits.MaxStorageGB,
            limits.MaxRecordingHoursPerMonth,
            limits.MaxDataRetentionDays,
            limits.AllowRecording,
            limits.AllowGuestAccess,
            limits.AllowCustomBranding
        };
    }
}
