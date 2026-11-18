namespace Yalfan.Domain.Common;

/// <summary>
/// The outcome of an operation that can fail for a business reason. Business failures travel
/// as values; exceptions are kept for bugs and unexpected failures.
/// </summary>
public class Result
{
    private readonly Error? _error;

    protected Result(Error? error)
    {
        _error = error;
    }

    public bool IsSuccess => _error is null;

    public bool IsFailure => _error is not null;

    /// <summary>The failure. Reading it on a successful result is a bug and throws.</summary>
    public Error Error =>
        _error ?? throw new InvalidOperationException("A successful result has no error.");

    public static Result Success() => new(error: null);

    public static Result Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result(error);
    }

    public static Result<T> Success<T>(T value) => new(value);

    public static Result<T> Failure<T>(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result<T>(error);
    }

    public static implicit operator Result(Error error) => Failure(error);
}

/// <summary>A <see cref="Result"/> that carries a value when it succeeds.</summary>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    internal Result(T value)
        : base(error: null)
    {
        _value = value;
    }

    internal Result(Error error)
        : base(error)
    {
    }

    /// <summary>The value. Reading it on a failed result is a bug and throws.</summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"A failed result has no value. Error: {Error.Code}.");

    public static implicit operator Result<T>(T value) => new(value);

    public static implicit operator Result<T>(Error error) => Failure<T>(error);
}
