using Yalfan.Domain.Audit;
using Yalfan.Domain.Identity;

namespace Yalfan.Domain.Tests.Audit;

public class AuditEntryTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 19, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void An_entry_with_a_user_is_made_by_that_user()
    {
        var userId = Guid.CreateVersion7();

        var entry = AuditEntry.Record(Now, userId, "trace", "users", "id", AuditAction.Updated, "{}");

        Assert.Equal(AuditActor.User, entry.Actor);
        Assert.Equal(userId, entry.UserId);
    }

    [Fact]
    public void An_entry_without_a_user_is_made_by_the_system()
    {
        var entry = AuditEntry.Record(Now, null, null, "users", "id", AuditAction.Created, "{}");

        Assert.Equal(AuditActor.System, entry.Actor);
    }

    [Theory]
    [InlineData("", "id", "{}")]
    [InlineData("users", "", "{}")]
    [InlineData("users", "id", "")]
    public void An_entry_needs_a_table_a_row_and_changes(string entityType, string entityId, string changes)
    {
        Assert.Throws<ArgumentException>(() =>
            AuditEntry.Record(Now, null, null, entityType, entityId, AuditAction.Created, changes));
    }

    [Fact]
    public void A_deactivated_user_is_no_longer_active()
    {
        var user = User.Create("Zahra", MobileNumber.Create("09121234567").Value, Role.Operator, Now).Value;

        user.Deactivate();

        Assert.False(user.IsActive);
    }
}
