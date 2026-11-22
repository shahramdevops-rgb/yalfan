namespace Yalfan.Domain.Tests.Common;

public class IbanTests
{
    /// <summary>The Iranian example of the SWIFT IBAN registry.</summary>
    private const string Valid = "IR710570029971601460641001";

    [Theory]
    [InlineData("IR710570029971601460641001")]
    [InlineData("ir710570029971601460641001")]
    [InlineData("710570029971601460641001")]
    [InlineData("IR71 0570 0299 7160 1460 6410 01")]
    [InlineData("IR71-0570-0299-7160-1460-6410-01")]
    [InlineData(" IR710570029971601460641001 ")]
    [InlineData("IR۷۱۰۵۷۰۰۲۹۹۷۱۶۰۱۴۶۰۶۴۱۰۰۱")]
    [InlineData("٧١٠٥٧٠٠٢٩٩٧١٦٠١٤٦٠٦٤١٠٠١")]
    public void Every_common_form_becomes_the_same_iban(string input)
    {
        var result = Iban.Create(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(Valid, result.Value.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("IR710570029971601460641002")]
    [InlineData("IR170570029971601460641001")]
    [InlineData("IR71057002997160146064100")]
    [InlineData("IR7105700299716014606410011")]
    [InlineData("DE89370400440532013000")]
    [InlineData("GB710570029971601460641001")]
    [InlineData("IR71057002997160146064100A")]
    [InlineData("6037991234567890")]
    public void Anything_else_is_refused(string? input)
    {
        var result = Iban.Create(input);

        Assert.True(result.IsFailure);
        Assert.Equal("iban.invalid", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public void The_masked_form_shows_only_the_last_four_digits()
    {
        var iban = Iban.Create("IR062960000000100324200001").Value;

        Assert.Equal("IR** **** **** **** **** **00 01", iban.Masked);
    }

    [Fact]
    public void Printing_an_iban_by_mistake_prints_the_masked_form()
    {
        var iban = Iban.Create(Valid).Value;

        Assert.Equal(iban.Masked, iban.ToString());
        Assert.DoesNotContain("0570", $"{iban}", StringComparison.Ordinal);
    }

    [Fact]
    public void Two_forms_of_the_same_iban_are_equal()
    {
        Assert.Equal(Iban.Create("710570029971601460641001").Value, Iban.Create(Valid).Value);
    }
}
