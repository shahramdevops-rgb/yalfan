using Microsoft.EntityFrameworkCore;
using Yalfan.Domain.Catalog;
using Yalfan.Domain.Identity;

namespace Yalfan.Application.Common;

/// <summary>
/// The database as the use cases see it. Each module adds its own sets here; a module changes
/// another module's data only through that module's handlers (ARCHITECTURE.md 5).
/// </summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }

    DbSet<OtpCode> OtpCodes { get; }

    DbSet<RefreshToken> RefreshTokens { get; }

    DbSet<Category> Categories { get; }

    DbSet<Fabric> Fabrics { get; }

    DbSet<Colour> Colours { get; }

    /// <summary>Models; their sizes and variants are reached through the model.</summary>
    DbSet<Product> Products { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
