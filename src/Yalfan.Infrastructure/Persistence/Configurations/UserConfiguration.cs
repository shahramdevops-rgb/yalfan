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

        // Sorted in Persian alphabetical order on the users screen, like contractors (ADR 0019).
        builder.Property(user => user.FullName)
            .HasMaxLength(User.FullNameMaxLength)
            .UseCollation(PersianCollation.Name)
            .IsRequired();

        // Stored as text ("Owner"), so a report or a hand-written query reads it without a lookup
        // table and reordering the enum never changes stored meaning.
        builder.Property(user => user.Role).HasConversion<string>().HasMaxLength(20);

        builder.Ignore(user => user.IsOwnerAccount);

        // A table of their own (ADR 0026): an owner account logs in with several numbers, each
        // switched on and off by itself, and each change is audited as a row of this table.
        builder.OwnsMany(user => user.LoginNumbers, number =>
        {
            number.ToTable("user_login_numbers");
            number.WithOwner().HasForeignKey("UserId");
            number.HasKey(x => x.Id);
            number.Property(x => x.Id).ValueGeneratedNever();

            number.Property(x => x.MobileNumber)
                .HasConversion(mobile => mobile.Value, value => MobileNumber.Create(value).Value)
                .HasMaxLength(11)
                .IsRequired();

            // One account per number, whoever holds it: the number is how a person logs in.
            number.HasIndex(x => x.MobileNumber).IsUnique();
        });

        builder.Navigation(user => user.LoginNumbers).HasField("_loginNumbers").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
