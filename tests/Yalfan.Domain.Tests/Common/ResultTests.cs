namespace Yalfan.Domain.Tests.Common;

public class ResultTests
{
    private static readonly Error SampleError = Error.Rule("sample.rule_broken");

    [Fact]
    public void Success_has_no_error()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Throws<InvalidOperationException>(() => result.Error);
    }

    [Fact]
    public void Failure_carries_its_error()
    {
        var result = Result.Failure(SampleError);

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Equal(SampleError, result.Error);
    }

    [Fact]
    public void A_value_becomes_a_successful_result()
    {
        Result<int> result = 42;

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void An_error_becomes_a_failed_result()
    {
        Result<int> typed = SampleError;
        Result untyped = SampleError;

        Assert.True(typed.IsFailure);
        Assert.Equal(SampleError, typed.Error);
        Assert.True(untyped.IsFailure);
    }

    [Fact]
    public void Reading_the_value_of_a_failed_result_throws_and_names_the_error()
    {
        var result = Result.Failure<int>(SampleError);

        var exception = Assert.Throws<InvalidOperationException>(() => result.Value);
        Assert.Contains("sample.rule_broken", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Errors_with_the_same_code_and_type_are_equal()
    {
        Assert.Equal(Error.NotFound("contractor.not_found"), Error.NotFound("contractor.not_found"));
        Assert.NotEqual(Error.NotFound("contractor.not_found"), Error.Conflict("contractor.not_found"));
    }

    [Fact]
    public void Each_factory_sets_its_error_type()
    {
        Assert.Equal(ErrorType.Validation, Error.Validation("a.b").Type);
        Assert.Equal(ErrorType.NotFound, Error.NotFound("a.b").Type);
        Assert.Equal(ErrorType.Conflict, Error.Conflict("a.b").Type);
        Assert.Equal(ErrorType.Forbidden, Error.Forbidden("a.b").Type);
        Assert.Equal(ErrorType.Rule, Error.Rule("a.b").Type);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void An_error_needs_a_code(string code)
    {
        Assert.Throws<ArgumentException>(() => Error.Validation(code));
    }
}
