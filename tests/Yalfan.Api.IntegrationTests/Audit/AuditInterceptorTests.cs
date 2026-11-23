using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Security;
using Yalfan.Domain.Audit;
using Yalfan.Domain.Common;
using Yalfan.Infrastructure.Persistence.Auditing;
using Yalfan.Infrastructure.Persistence.Configurations;
using Yalfan.Infrastructure.Persistence.Interceptors;

namespace Yalfan.Api.IntegrationTests.Audit;

/// <summary>
/// The interceptor on a small DbContext of its own, for cases the product has no entity for yet:
/// redacted fields, excluded entities, deletes and failed saves.
/// </summary>
public sealed class AuditInterceptorTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 11, 19, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid UserId = Guid.CreateVersion7();

    private string _connectionString = string.Empty;

    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _connectionString = await ApiFactory.CreateDatabaseConnectionStringAsync();
        await using var db = NewContext();
        await db.Database.EnsureCreatedAsync(Cancel);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task A_redacted_field_is_recorded_as_changed_without_its_values()
    {
        await using var db = NewContext();
        var gadget = new Gadget("first", "IR000000000000000000000001");
        db.Gadgets.Add(gadget);
        await db.SaveChangesAsync(Cancel);

        gadget.Secret = "IR000000000000000000000002";
        await db.SaveChangesAsync(Cancel);

        var entries = await db.AuditEntries.Where(e => e.EntityId == gadget.Id.ToString()).OrderBy(e => e.Action).ToListAsync(Cancel);
        var created = Parse(entries.Single(e => e.Action == AuditAction.Created).Changes);
        var updated = Parse(entries.Single(e => e.Action == AuditAction.Updated).Changes);

        Assert.Equal(JsonValueKind.Null, created.GetProperty("secret").GetProperty("new").ValueKind);
        Assert.True(created.GetProperty("secret").GetProperty("redacted").GetBoolean());
        Assert.Equal(JsonValueKind.Null, updated.GetProperty("secret").GetProperty("old").ValueKind);
        Assert.True(updated.GetProperty("secret").GetProperty("redacted").GetBoolean());
        Assert.DoesNotContain("IR0000", string.Concat(entries.Select(e => e.Changes)), StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_excluded_entity_is_not_recorded()
    {
        await using var db = NewContext();
        var ignored = new Ignored("noise");
        db.Ignored.Add(ignored);

        await db.SaveChangesAsync(Cancel);

        Assert.False(await db.AuditEntries.AnyAsync(e => e.EntityId == ignored.Id.ToString(), Cancel));
    }

    [Fact]
    public async Task A_delete_records_the_old_values_and_the_user()
    {
        await using var db = NewContext();
        var gadget = new Gadget("to-delete", "secret");
        db.Gadgets.Add(gadget);
        await db.SaveChangesAsync(Cancel);

        db.Gadgets.Remove(gadget);
        await db.SaveChangesAsync(Cancel);

        var deleted = await db.AuditEntries.SingleAsync(
            e => e.EntityId == gadget.Id.ToString() && e.Action == AuditAction.Deleted, Cancel);
        var changes = Parse(deleted.Changes);
        Assert.Equal("to-delete", changes.GetProperty("name").GetProperty("old").GetString());
        Assert.False(changes.GetProperty("name").TryGetProperty("new", out _));
        Assert.Equal(UserId, deleted.UserId);
        Assert.Equal(AuditActor.User, deleted.Actor);
        Assert.Equal(Now, deleted.OccurredAt);
    }

    [Fact]
    public async Task A_failed_save_leaves_no_audit_entry_behind()
    {
        await using var db = NewContext();
        db.Gadgets.Add(new Gadget("unique-name", "s"));
        await db.SaveChangesAsync(Cancel);
        var before = await db.AuditEntries.CountAsync(Cancel);

        var duplicate = new Gadget("unique-name", "s");
        db.Gadgets.Add(duplicate);
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(Cancel));

        // Nothing waits in memory to be saved by the next attempt.
        Assert.DoesNotContain(db.ChangeTracker.Entries<AuditEntry>(), e => e.State == EntityState.Added);

        db.Entry(duplicate).State = EntityState.Detached;
        db.Gadgets.Add(new Gadget("another-name", "s"));
        await db.SaveChangesAsync(Cancel);

        Assert.Equal(before + 1, await db.AuditEntries.CountAsync(Cancel));
    }

    [Fact]
    public async Task The_parts_of_a_value_object_in_the_row_are_fields_named_after_their_columns()
    {
        await using var db = NewContext();
        var priced = new Priced(Money.Rial(100));
        db.Priced.Add(priced);
        await db.SaveChangesAsync(Cancel);

        priced.Price = Money.Rial(250);
        await db.SaveChangesAsync(Cancel);

        var entries = await db.AuditEntries.Where(e => e.EntityId == priced.Id.ToString()).ToListAsync(Cancel);
        var created = Parse(entries.Single(e => e.Action == AuditAction.Created).Changes);
        var updated = Parse(entries.Single(e => e.Action == AuditAction.Updated).Changes);

        Assert.Equal(100, created.GetProperty("priceAmount").GetProperty("new").GetInt64());
        Assert.Equal("IRR", created.GetProperty("priceCurrency").GetProperty("new").GetString());

        // Only the part that changed, as for any other field.
        var amount = Assert.Single(updated.EnumerateObject());
        Assert.Equal("priceAmount", amount.Name);
        Assert.Equal(100, amount.Value.GetProperty("old").GetInt64());
        Assert.Equal(250, amount.Value.GetProperty("new").GetInt64());
    }

    [Fact]
    public async Task Changing_or_deleting_an_audit_entry_in_code_is_refused()
    {
        await using var db = NewContext();
        db.Gadgets.Add(new Gadget("audited", "s"));
        await db.SaveChangesAsync(Cancel);
        var entry = await db.AuditEntries.FirstAsync(Cancel);

        db.Entry(entry).Property(e => e.Changes).CurrentValue = "{}";
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync(Cancel));

        db.Entry(entry).State = EntityState.Unchanged;
        db.AuditEntries.Remove(entry);
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync(Cancel));
    }

    private AuditTestDbContext NewContext() => new(new DbContextOptionsBuilder<AuditTestDbContext>()
        .UseNpgsql(_connectionString)
        .UseSnakeCaseNamingConvention()
        .AddInterceptors(new AuditInterceptor(new FixedUser(), new FixedClock()))
        .Options);

    private static JsonElement Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private sealed class FixedUser : ICurrentUser
    {
        public Guid? UserId => AuditInterceptorTests.UserId;
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => Now;

        public DateOnly Today => DateOnly.FromDateTime(Now.UtcDateTime);
    }

    private sealed class AuditTestDbContext(DbContextOptions<AuditTestDbContext> options) : DbContext(options)
    {
        public DbSet<Gadget> Gadgets => Set<Gadget>();

        public DbSet<Ignored> Ignored => Set<Ignored>();

        public DbSet<Priced> Priced => Set<Priced>();

        public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new AuditEntryConfiguration());

            modelBuilder.Entity<Gadget>(gadget =>
            {
                gadget.HasKey(g => g.Id);
                gadget.HasIndex(g => g.Name).IsUnique();
                gadget.Property(g => g.Secret).IsAuditRedacted();
            });

            modelBuilder.Entity<Ignored>(ignored =>
            {
                ignored.HasKey(i => i.Id);
                ignored.ExcludeFromAudit();
            });

            // Money as the product maps it (ADR 0002), with the default column names.
            modelBuilder.Entity<Priced>(priced =>
            {
                priced.HasKey(p => p.Id);
                priced.ComplexProperty(p => p.Price, money =>
                {
                    money.Property(m => m.Amount);
                    money.Property(m => m.Currency)
                        .HasConversion(currency => currency.Code, code => Currency.FromCode(code).Value);
                });
            });
        }
    }
}

public sealed class Gadget(string name, string secret) : Entity
{
    public string Name { get; set; } = name;

    public string Secret { get; set; } = secret;
}

public sealed class Ignored(string note) : Entity
{
    public string Note { get; set; } = note;
}

public sealed class Priced(Money price) : Entity
{
    private Priced()
        : this(Money.Rial(0))
    {
    }

    public Money Price { get; set; } = price;
}
