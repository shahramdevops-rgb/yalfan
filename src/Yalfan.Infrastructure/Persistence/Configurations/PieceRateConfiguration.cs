using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Yalfan.Domain.Catalog;
using Yalfan.Domain.Common;
using Yalfan.Domain.Production;

namespace Yalfan.Infrastructure.Persistence.Configurations;

/// <summary>
/// The rate card (ADR 0021). The table is append-only: the migration that creates it adds a
/// trigger that refuses UPDATE, DELETE and TRUNCATE, so an old rate can never be overwritten,
/// even by a bug or a hand-written query.
/// </summary>
internal sealed class PieceRateConfiguration : IEntityTypeConfiguration<PieceRate>
{
    public const string TableName = "piece_rates";

    public void Configure(EntityTypeBuilder<PieceRate> builder)
    {
        builder.ToTable(TableName, table =>
            table.HasCheckConstraint("ck_piece_rates_amount_positive", "amount > 0"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        // Restrict: a model or a stage with rates must never disappear under them.
        builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Stage>().WithMany().HasForeignKey(x => x.StageId).OnDelete(DeleteBehavior.Restrict);

        // The first money in the database (ADR 0002, point 8): a bigint of Rial and a 3-letter
        // currency code, as plain columns of the row.
        builder.ComplexProperty(x => x.Amount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("amount");
            money.Property(m => m.Currency)
                .HasConversion(currency => currency.Code, code => Currency.FromCode(code).Value)
                .HasColumnName("currency")
                .HasMaxLength(3);
        });

        // A calendar day with no time: PostgreSQL date (ADR 0005).
        builder.Property(x => x.StartsOn);
        builder.Property(x => x.EnteredAt);

        // The rate card of one model is read by stage and start date.
        builder.HasIndex(x => new { x.ProductId, x.StageId, x.StartsOn });
    }
}
