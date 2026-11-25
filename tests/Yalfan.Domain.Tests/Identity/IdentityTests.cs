using Yalfan.Domain.Identity;

namespace Yalfan.Domain.Tests.Identity;

public class UserTests
{
    private static readonly MobileNumber Mobile = MobileNumber.Create("09121234567").Value;
    private static readonly MobileNumber Other = MobileNumber.Create("09351234567").Value;
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_new_user_is_active_and_keeps_the_trimmed_name()
    {
        var user = User.Create("  Zahra Ahmadi ", Mobile, Role.Operator, Now).Value;

        Assert.Equal("Zahra Ahmadi", user.FullName);
        Assert.True(user.IsActive);
        Assert.Equal(Role.Operator, user.Role);
        Assert.Equal(Now, user.CreatedAt);
    }

    [Theory]
    [InlineData(null, "identity.full_name_required")]
    [InlineData("   ", "identity.full_name_required")]
    public void A_user_needs_a_name(string? name, string code)
    {
        Assert.Equal(code, User.Create(name, Mobile, Role.Operator, Now).Error.Code);
    }

    [Fact]
    public void A_name_has_a_maximum_length()
    {
        var name = new string('x', User.FullNameMaxLength + 1);

        Assert.Equal("identity.full_name_too_long", User.Create(name, Mobile, Role.Operator, Now).Error.Code);
    }

    [Fact]
    public void A_new_user_logs_in_with_one_switched_on_number()
    {
        var user = User.Create("Zahra", Mobile, Role.Operator, Now).Value;

        var number = Assert.Single(user.LoginNumbers);
        Assert.Equal(Mobile, number.MobileNumber);
        Assert.True(number.IsActive);
        Assert.True(user.HasLoginNumber(Mobile));
    }

    [Fact]
    public void A_rename_keeps_the_trimmed_name_and_refuses_an_empty_one()
    {
        var user = User.Create("Zahra", Mobile, Role.Operator, Now).Value;

        Assert.True(user.Rename("  Zahra Karimi ").IsSuccess);
        Assert.Equal("Zahra Karimi", user.FullName);
        Assert.Equal(IdentityErrors.FullNameRequired, user.Rename(" ").Error);
        Assert.Equal("Zahra Karimi", user.FullName);
    }

    [Fact]
    public void The_role_moves_between_manager_and_operator_but_never_to_owner()
    {
        var user = User.Create("Zahra", Mobile, Role.Operator, Now).Value;

        Assert.True(user.ChangeRole(Role.Manager).IsSuccess);
        Assert.Equal(Role.Manager, user.Role);
        Assert.Equal(IdentityErrors.RoleNotAllowed, user.ChangeRole(Role.Owner).Error);
        Assert.Equal(Role.Manager, user.Role);
    }

    [Fact]
    public void A_manager_gets_a_new_number_in_place_of_the_old_one()
    {
        var user = User.Create("Zahra", Mobile, Role.Manager, Now).Value;
        var numberId = user.LoginNumbers.Single().Id;

        Assert.True(user.ChangeMobileNumber(Other).IsSuccess);

        var number = Assert.Single(user.LoginNumbers);
        Assert.Equal(numberId, number.Id);
        Assert.Equal(Other, number.MobileNumber);
        Assert.False(user.HasLoginNumber(Mobile));
    }

    [Fact]
    public void An_operator_is_deactivated_and_activated_again()
    {
        var user = User.Create("Zahra", Mobile, Role.Operator, Now).Value;

        Assert.True(user.Deactivate().IsSuccess);
        Assert.False(user.IsActive);
        Assert.True(user.Activate().IsSuccess);
        Assert.True(user.IsActive);
    }

    [Fact]
    public void An_owner_account_keeps_its_role_active_flag_and_numbers()
    {
        var owner = User.Create("Owner", Mobile, Role.Owner, Now).Value;

        Assert.True(owner.IsOwnerAccount);
        Assert.Equal(IdentityErrors.OwnerLocked, owner.ChangeRole(Role.Manager).Error);
        Assert.Equal(IdentityErrors.OwnerLocked, owner.Deactivate().Error);
        Assert.Equal(IdentityErrors.OwnerLocked, owner.Activate().Error);
        Assert.Equal(IdentityErrors.OwnerLocked, owner.ChangeMobileNumber(Other).Error);
        Assert.Equal(Role.Owner, owner.Role);
        Assert.True(owner.IsActive);
        Assert.Equal(Mobile, owner.LoginNumbers.Single().MobileNumber);
    }

    [Fact]
    public void An_owner_account_gets_more_numbers_and_a_number_it_has_is_left_as_it_is()
    {
        var owner = User.Create("Owner", Mobile, Role.Owner, Now).Value;
        Assert.True(owner.AddLoginNumber(Other).IsSuccess);
        var other = owner.LoginNumbers.Single(n => n.MobileNumber == Other);
        Assert.True(owner.DeactivateLoginNumber(other.Id).IsSuccess);

        // As at a restart, when the configuration lists it again.
        Assert.True(owner.AddLoginNumber(Other).IsSuccess);

        Assert.Equal(2, owner.LoginNumbers.Count);
        Assert.False(other.IsActive);
    }

    [Fact]
    public void Only_an_owner_account_has_more_than_one_number()
    {
        var operatorUser = User.Create("Zahra", Mobile, Role.Operator, Now).Value;

        Assert.Equal(IdentityErrors.SingleLoginNumber, operatorUser.AddLoginNumber(Other).Error);
        Assert.Single(operatorUser.LoginNumbers);
    }

    [Fact]
    public void A_number_is_switched_off_and_on_but_the_last_one_switched_on_stays_on()
    {
        var owner = User.Create("Owner", Mobile, Role.Owner, Now).Value;
        owner.AddLoginNumber(Other);
        var first = owner.LoginNumbers.Single(n => n.MobileNumber == Mobile);
        var second = owner.LoginNumbers.Single(n => n.MobileNumber == Other);

        Assert.True(owner.DeactivateLoginNumber(first.Id).IsSuccess);
        Assert.Equal(IdentityErrors.LastLoginNumber, owner.DeactivateLoginNumber(second.Id).Error);
        Assert.True(second.IsActive);

        // Switching off one already off changes nothing and is not refused.
        Assert.True(owner.DeactivateLoginNumber(first.Id).IsSuccess);

        Assert.True(owner.ActivateLoginNumber(first.Id).IsSuccess);
        Assert.True(owner.DeactivateLoginNumber(second.Id).IsSuccess);
        Assert.True(first.IsActive);
        Assert.False(second.IsActive);
    }

    [Fact]
    public void A_single_number_cannot_be_switched_off()
    {
        var developer = User.Create("Developer", Mobile, Role.Owner, Now).Value;
        var manager = User.Create("Manager", Other, Role.Manager, Now).Value;

        Assert.Equal(IdentityErrors.LastLoginNumber, developer.DeactivateLoginNumber(developer.LoginNumbers.Single().Id).Error);
        Assert.Equal(IdentityErrors.LastLoginNumber, manager.DeactivateLoginNumber(manager.LoginNumbers.Single().Id).Error);
    }

    [Fact]
    public void An_unknown_number_is_not_found()
    {
        var owner = User.Create("Owner", Mobile, Role.Owner, Now).Value;

        Assert.Equal(IdentityErrors.LoginNumberNotFound, owner.DeactivateLoginNumber(Guid.CreateVersion7()).Error);
        Assert.Equal(IdentityErrors.LoginNumberNotFound, owner.ActivateLoginNumber(Guid.CreateVersion7()).Error);
    }
}

public class OtpCodeTests
{
    private static readonly Guid UserId = Guid.CreateVersion7();
    private static readonly MobileNumber Mobile = MobileNumber.Create("09121234567").Value;
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void The_right_code_is_accepted_once()
    {
        var otp = OtpCode.Issue(UserId, Mobile, "123456", Now);

        Assert.True(otp.Verify("123456", Now.AddSeconds(30)).IsSuccess);
        Assert.Equal(IdentityErrors.OtpExpired, otp.Verify("123456", Now.AddSeconds(31)).Error);
    }

    [Fact]
    public void The_code_itself_is_not_stored()
    {
        var otp = OtpCode.Issue(UserId, Mobile, "123456", Now);

        Assert.DoesNotContain("123456", otp.CodeHash, StringComparison.Ordinal);
    }

    [Fact]
    public void The_same_code_hashes_differently_for_another_user()
    {
        var first = OtpCode.Issue(UserId, Mobile, "123456", Now);
        var second = OtpCode.Issue(Guid.CreateVersion7(), Mobile, "123456", Now);

        Assert.NotEqual(first.CodeHash, second.CodeHash);
    }

    [Fact]
    public void The_same_code_hashes_differently_for_another_number()
    {
        var first = OtpCode.Issue(UserId, Mobile, "123456", Now);
        var second = OtpCode.Issue(UserId, MobileNumber.Create("09351234567").Value, "123456", Now);

        Assert.NotEqual(first.CodeHash, second.CodeHash);
        Assert.Equal(Mobile, first.MobileNumber);
    }

    [Fact]
    public void A_code_is_dead_at_the_end_of_its_lifetime()
    {
        var otp = OtpCode.Issue(UserId, Mobile, "123456", Now);

        Assert.True(otp.IsUsable(Now + OtpCode.Lifetime - TimeSpan.FromTicks(1)));
        Assert.Equal(IdentityErrors.OtpExpired, otp.Verify("123456", Now + OtpCode.Lifetime).Error);
    }

    [Fact]
    public void Five_wrong_tries_kill_the_code()
    {
        var otp = OtpCode.Issue(UserId, Mobile, "123456", Now);

        for (var attempt = 0; attempt < OtpCode.MaxFailedAttempts; attempt++)
        {
            Assert.Equal(IdentityErrors.OtpInvalid, otp.Verify("654321", Now).Error);
        }

        Assert.Equal(IdentityErrors.OtpExpired, otp.Verify("123456", Now).Error);
    }
}

public class RefreshTokenTests
{
    private static readonly Guid UserId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 10, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Week = TimeSpan.FromDays(7);

    [Fact]
    public void Rotation_revokes_the_old_token_and_keeps_the_family()
    {
        var first = RefreshToken.StartFamily(UserId, "hash-1", Now, Week);

        var next = first.Rotate("hash-2", Now.AddMinutes(10), Week);

        Assert.False(first.IsActive(Now.AddMinutes(10)));
        Assert.Equal(next.Id, first.ReplacedById);
        Assert.Equal(first.FamilyId, next.FamilyId);
        Assert.Equal(Now.AddMinutes(10) + Week, next.ExpiresAt);
        Assert.True(next.IsActive(Now.AddMinutes(10)));
    }

    [Fact]
    public void A_just_rotated_token_is_recognised_for_thirty_seconds()
    {
        var first = RefreshToken.StartFamily(UserId, "hash-1", Now, Week);
        first.Rotate("hash-2", Now, Week);

        Assert.True(first.WasJustRotated(Now + RefreshToken.ReuseGrace));
        Assert.False(first.WasJustRotated(Now + RefreshToken.ReuseGrace + TimeSpan.FromTicks(1)));
    }

    [Fact]
    public void A_revoked_token_was_not_rotated()
    {
        var token = RefreshToken.StartFamily(UserId, "hash-1", Now, Week);
        token.Revoke(Now);

        Assert.False(token.IsActive(Now));
        Assert.False(token.WasJustRotated(Now));
    }

    [Fact]
    public void An_expired_or_revoked_token_cannot_be_rotated()
    {
        var expired = RefreshToken.StartFamily(UserId, "hash-1", Now, Week);
        var revoked = RefreshToken.StartFamily(UserId, "hash-2", Now, Week);
        revoked.Revoke(Now);

        Assert.Throws<InvalidOperationException>(() => expired.Rotate("hash-3", Now + Week, Week));
        Assert.Throws<InvalidOperationException>(() => revoked.Rotate("hash-4", Now, Week));
    }

    [Fact]
    public void Revoking_twice_keeps_the_first_time()
    {
        var token = RefreshToken.StartFamily(UserId, "hash-1", Now, Week);
        token.Revoke(Now);
        token.Revoke(Now.AddHours(1));

        Assert.Equal(Now, token.RevokedAt);
    }
}
