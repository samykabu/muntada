using Microsoft.EntityFrameworkCore;
using Muntada.Identity.Domain.EmailVerification;
using Muntada.Identity.Domain.GuestLink;
using Muntada.Identity.Domain.Otp;
using Muntada.Identity.Domain.PasswordReset;
using Muntada.Identity.Domain.Pat;
using Muntada.Identity.Domain.Session;
using Muntada.Identity.Domain.User;

namespace Muntada.Identity.Infrastructure;

/// <summary>
/// Entity Framework Core DbContext for the Identity module.
/// All tables are created under the <c>[identity]</c> SQL Server schema
/// per Constitution I (Modular Monolith Discipline).
/// </summary>
public class IdentityDbContext : DbContext
{
    /// <summary>
    /// The SQL Server schema name for identity module tables.
    /// </summary>
    public const string SchemaName = "identity";

    /// <summary>Gets or sets the Users DbSet.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>Gets or sets the Sessions DbSet.</summary>
    public DbSet<Session> Sessions => Set<Session>();

    /// <summary>Gets or sets the RefreshTokens DbSet.</summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <summary>Gets or sets the OtpChallenges DbSet.</summary>
    public DbSet<OtpChallenge> OtpChallenges => Set<OtpChallenge>();

    /// <summary>Gets or sets the GuestMagicLinks DbSet.</summary>
    public DbSet<GuestMagicLink> GuestMagicLinks => Set<GuestMagicLink>();

    /// <summary>Gets or sets the PersonalAccessTokens DbSet.</summary>
    public DbSet<PersonalAccessToken> PersonalAccessTokens => Set<PersonalAccessToken>();

    /// <summary>Gets or sets the PasswordResetTokens DbSet.</summary>
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    /// <summary>Gets or sets the EmailVerificationTokens DbSet.</summary>
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();

    /// <summary>
    /// Initializes a new instance of <see cref="IdentityDbContext"/>.
    /// </summary>
    /// <param name="options">The DbContext options.</param>
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options) { }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        base.OnModelCreating(modelBuilder);

        // User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.OwnsOne(e => e.Email, email =>
            {
                email.Property(e => e.Value).HasColumnName("Email").HasMaxLength(320);
                email.HasIndex(e => e.Value).IsUnique();
            });
            entity.OwnsOne(e => e.PasswordHash, ph => ph.Property(p => p.Hash).HasColumnName("PasswordHash").HasMaxLength(512));
            entity.OwnsOne(e => e.PhoneNumber, pn => pn.Property(p => p.Value).HasColumnName("PhoneNumber").HasMaxLength(20));
            entity.Property(e => e.Status).HasConversion<int>();
        });

        // Session entity
        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.OwnsOne(e => e.DeviceInfo);
            entity.Property(e => e.Status).HasConversion<int>();
            entity.HasIndex(e => e.UserId);
        });

        // RefreshToken entity
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TokenHash).HasMaxLength(512);
            entity.Property(e => e.Status).HasConversion<int>();
            entity.HasIndex(e => e.SessionId);
        });

        // OtpChallenge entity
        modelBuilder.Entity<OtpChallenge>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.CodeHash).HasMaxLength(128);
            entity.Property(e => e.Status).HasConversion<int>();
        });

        // GuestMagicLink entity
        modelBuilder.Entity<GuestMagicLink>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TokenHash).HasMaxLength(128);
            entity.Property(e => e.Status).HasConversion<int>();
            entity.HasIndex(e => e.TokenHash);
        });

        // PersonalAccessToken entity
        modelBuilder.Entity<PersonalAccessToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TokenHash).HasMaxLength(512);
            entity.Property(e => e.Name).HasMaxLength(256);
            entity.Property(e => e.Status).HasConversion<int>();
            entity.HasIndex(e => e.UserId);
        });

        // PasswordResetToken entity
        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TokenHash).HasMaxLength(128);
            entity.Property(e => e.Status).HasConversion<int>();
            entity.HasIndex(e => e.UserId);
        });

        // EmailVerificationToken entity
        modelBuilder.Entity<EmailVerificationToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TokenHash).HasMaxLength(128);
            entity.Property(e => e.Status).HasConversion<int>();
            entity.HasIndex(e => e.UserId);
        });
    }
}
