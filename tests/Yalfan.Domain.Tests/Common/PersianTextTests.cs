using Yalfan.Domain.Common;

namespace Yalfan.Domain.Tests.Common;

public class PersianTextTests
{
    [Theory]
    [InlineData("کیف", "کیف")]
    [InlineData("  کیف   دوشی  ", "کیف دوشی")]
    [InlineData("كيف", "کیف")]
    [InlineData("مشكى", "مشکی")]
    [InlineData("کیف\tدستی", "کیف دستی")]
    public void Names_typed_on_any_keyboard_come_out_the_same(string typed, string expected)
    {
        Assert.Equal(expected, PersianText.Normalize(typed));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Nothing_typed_is_empty(string? typed)
    {
        Assert.Equal(string.Empty, PersianText.Normalize(typed));
    }

    [Fact]
    public void Digits_and_the_zero_width_non_joiner_stay_as_typed()
    {
        const string typed = "کیف‌دستی ۳۰";

        Assert.Equal(typed, PersianText.Normalize(typed));
    }
}
