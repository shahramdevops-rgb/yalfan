namespace Yalfan.Domain.Tests.Common;

public class ValidationErrorTests
{
    [Fact]
    public void A_validation_error_has_one_code_and_lists_its_fields()
    {
        var error = new ValidationError([new FieldError("Name", "validation.required")]);

        Assert.Equal("validation.failed", error.Code);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("Name", Assert.Single(error.Errors).Field);
    }

    [Fact]
    public void A_validation_error_needs_at_least_one_field()
    {
        Assert.Throws<ArgumentException>(() => new ValidationError([]));
    }

    [Fact]
    public void A_validation_error_becomes_a_failed_result()
    {
        Result<int> result = new ValidationError([new FieldError("Name", "validation.required")]);

        Assert.IsType<ValidationError>(result.Error);
    }

    [Fact]
    public void A_validation_error_prints_as_its_code()
    {
        var error = new ValidationError([new FieldError("Name", "validation.required")]);

        Assert.Equal("validation.failed", error.ToString());
    }

    [Fact]
    public void A_field_error_without_args_has_an_empty_set()
    {
        Assert.Empty(new FieldError("Name", "validation.required").Args);
    }

    [Fact]
    public void A_field_error_on_the_whole_request_has_an_empty_field()
    {
        Assert.Equal(string.Empty, new FieldError(string.Empty, "sample.rule").Field);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_field_error_needs_a_code(string code)
    {
        Assert.Throws<ArgumentException>(() => new FieldError("Name", code));
    }
}
