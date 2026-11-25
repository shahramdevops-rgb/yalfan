using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Yalfan.Domain.Common;
using Yalfan.Domain.Identity;
using Yalfan.Infrastructure.Persistence.Auditing;

namespace Yalfan.Infrastructure.Persistence.Configurations;

internal sealed class OtpCodeConfiguration : IEntityTypeConfiguration<OtpCode>
{
    public void Configure(EntityTypeBuilder<OtpCode> builder)
    {
        builder.ToTable("otp_codes");

        // A security record of its own, written on every login; its hash must not be copied.
        builder.ExcludeFromAudit();
        builder.HasKey(code => code.Id);
        builder.Property(code => code.Id).ValueGeneratedNever();

        builder.Property(code => code.CodeHash).HasMaxLength(64).IsRequired();

        builder.Property(code => code.MobileNumber)
            .HasConversion(mobile => mobile.Value, value => MobileNumber.Create(value).Value)
            .HasMaxLength(11)
            .IsRequired();

        builder.HasOne<User>().WithMany().HasForeignKey(code => code.UserId).OnDelete(DeleteBehavior.Restrict);

        // "Newest code of this user for this number" and "codes of this number in the last hour"
        // (ADR 0026: a code, its wait and its limit belong to one number).
        builder.HasIndex(code => new { code.UserId, code.MobileNumber, code.CreatedAt });

        // PostgreSQL's xmin as a row version: two requests with the same right code cannot both
        // use it.
        builder.Property<uint>("Version").IsRowVersion();
    }
}
