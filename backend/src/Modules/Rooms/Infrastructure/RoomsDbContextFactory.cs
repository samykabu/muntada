using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Muntada.Rooms.Infrastructure;

/// <summary>
/// Design-time factory for <see cref="RoomsDbContext"/>.
/// Used by EF Core CLI tools (dotnet ef migrations add) to create
/// the DbContext without requiring the full application DI container.
/// </summary>
public class RoomsDbContextFactory : IDesignTimeDbContextFactory<RoomsDbContext>
{
    /// <inheritdoc />
    public RoomsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<RoomsDbContext>();
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=MuntadaDesignTime;Trusted_Connection=True;");

        return new RoomsDbContext(optionsBuilder.Options);
    }
}
