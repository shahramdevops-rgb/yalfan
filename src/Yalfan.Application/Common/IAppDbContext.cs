using Microsoft.EntityFrameworkCore;
using Yalfan.Domain.Audit;
using Yalfan.Domain.Catalog;
using Yalfan.Domain.Identity;
using Yalfan.Domain.Parties;
using Yalfan.Domain.Production;

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

    /// <summary>The shared list of stages (ADR 0018).</summary>
    DbSet<Stage> Stages { get; }

    /// <summary>The rate card: append-only piece rates per model and stage (ADR 0021).</summary>
    DbSet<PieceRate> PieceRates { get; }

    /// <summary>Outside contractors; the stages each one does are reached through him (ADR 0019).</summary>
    DbSet<Contractor> Contractors { get; }

    /// <summary>In-house workers; the stages each one does are reached through the worker (ADR 0020).</summary>
    DbSet<Worker> Workers { get; }

    /// <summary>
    /// The audit, to read only: not a DbSet, so a use case can neither add nor change an entry
    /// (ADR 0013); the interceptor alone writes it. Read without tracking.
    /// </summary>
    IQueryable<AuditEntry> AuditEntries { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
