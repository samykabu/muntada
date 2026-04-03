using Microsoft.EntityFrameworkCore;

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

        // Entity configurations will be applied here as entities are added
    }
}
