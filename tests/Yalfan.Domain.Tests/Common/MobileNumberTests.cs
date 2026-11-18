namespace Yalfan.Domain.Tests.Common;

public class MobileNumberTests
{
    [Theory]
    [InlineData("09121234567")]
    [InlineData("9121234567")]
    [InlineData("+989121234567")]
    [InlineData("00989121234567")]
    [InlineData("989121234567")]
    [InlineData("0912 123 4567")]
    [InlineData("0912-123-4567")]
    [InlineData("(0912) 1234567")]
    [InlineData("۰۹۱۲۱۲۳۴۵۶۷")]
    [InlineData("٠٩١٢١٢٣٤٥٦٧")]
    [InlineData(" 09121234567 ")]
    public void Every_common_form_becomes_the_same_number(string input)
    {
        var result = MobileNumber.Create(input);

        Assert.True(result.IsSuccess);
        Assert.Equal("09121234567", result.Value.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("02112345678")]
    [InlineData("0912123456")]
    [InlineData("091212345678")]
    [InlineData("08121234567")]
    [InlineData("0912abc4567")]
    [InlineData("+19121234567")]
    public void Anything_else_is_refused(string? input)
    {
        var result = MobileNumber.Create(input);

        Assert.True(result.IsFailure);
        Assert.Equal("mobile.invalid", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public void Two_forms_of_the_same_number_are_equal()
    {
        Assert.Equal(MobileNumber.Create("+989121234567").Value, MobileNumber.Create("09121234567").Value);
    }

    [Fact]
    public void Digits_turns_persian_and_arabic_digits_into_latin()
    {
        Assert.Equal("0123456789-0123456789-x", Digits.ToLatin("۰۱۲۳۴۵۶۷۸۹-٠١٢٣٤٥٦٧٨٩-x"));
    }
}
