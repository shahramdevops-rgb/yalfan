using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Yalfan.Domain.Common;
using Yalfan.Domain.Identity;

namespace Yalfan.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).ValueGeneratedNever();

        builder.Property(user => user.FullName).HasMaxLength(User.FullNameMaxLength).IsRequired();

        builder.Property(user => user.MobileNumber)
            .HasConversion(mobile => mobile.Value, value => MobileNumber.Create(value).Value)
            .HasMaxLength(11)
            .IsRequired();

        // One account per mobile: the number is how a person logs in.
        builder.HasIndex(user => user.MobileNumber).IsUnique();

        // Stored as text ("Owner"), so a report or a hand-written query reads it without a lookup
        // table and reordering the enum never changes stored meaning.
        builder.Property(user => user.Role).HasConversion<string>().HasMaxLength(20);
    }
}
