using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
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

        builder.HasOne<User>().WithMany().HasForeignKey(code => code.UserId).OnDelete(DeleteBehavior.Restrict);

        // "Newest code of this user" and "codes of this user in the last hour".
        builder.HasIndex(code => new { code.UserId, code.CreatedAt });

        // PostgreSQL's xmin as a row version: two requests with the same right code cannot both
        // use it.
        builder.Property<uint>("Version").IsRowVersion();
    }
}
