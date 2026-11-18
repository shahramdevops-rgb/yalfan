using Yalfan.Domain.Identity;

namespace Yalfan.Domain.Tests.Identity;

public class UserTests
{
    private static readonly MobileNumber Mobile = MobileNumber.Create("09121234567").Value;
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
}

public class OtpCodeTests
{
    private static readonly Guid UserId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void The_right_code_is_accepted_once()
    {
        var otp = OtpCode.Issue(UserId, "123456", Now);

        Assert.True(otp.Verify("123456", Now.AddSeconds(30)).IsSuccess);
        Assert.Equal(IdentityErrors.OtpExpired, otp.Verify("123456", Now.AddSeconds(31)).Error);
    }

    [Fact]
    public void The_code_itself_is_not_stored()
    {
        var otp = OtpCode.Issue(UserId, "123456", Now);

        Assert.DoesNotContain("123456", otp.CodeHash, StringComparison.Ordinal);
    }

    [Fact]
    public void The_same_code_hashes_differently_for_another_user()
    {
        var first = OtpCode.Issue(UserId, "123456", Now);
        var second = OtpCode.Issue(Guid.CreateVersion7(), "123456", Now);

        Assert.NotEqual(first.CodeHash, second.CodeHash);
    }

    [Fact]
    public void A_code_is_dead_at_the_end_of_its_lifetime()
    {
        var otp = OtpCode.Issue(UserId, "123456", Now);

        Assert.True(otp.IsUsable(Now + OtpCode.Lifetime - TimeSpan.FromTicks(1)));
        Assert.Equal(IdentityErrors.OtpExpired, otp.Verify("123456", Now + OtpCode.Lifetime).Error);
    }

    [Fact]
    public void Five_wrong_tries_kill_the_code()
    {
        var otp = OtpCode.Issue(UserId, "123456", Now);

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
