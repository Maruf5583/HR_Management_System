namespace Hrms.Shared.Primitives;

public readonly record struct Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static Error Validation(string message) => new("Validation.Error", message);
    public static Error NotFound(string message) => new("Record.NotFound", message);
    public static Error Conflict(string message) => new("Record.Conflict", message);
    public static Error Failure(string message) => new("Operation.Failure", message);
}

public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None || !isSuccess && error == Error.None)
        {
            throw new InvalidOperationException("Invalid result state.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    public static Result Failure(string message) => new(false, Error.Failure(message));
    public static Result Failure(string code, string message) => new(false, new Error(code, message));

    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);
    public static Result<T> Failure<T>(string message) => Result<T>.Failure(Error.Failure(message));
}

public class Result<T> : Result
{
    private readonly T? _value;

    private Result(bool isSuccess, T? value, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value on failure result.");

    public static Result<T> Success(T value) => new(true, value, Error.None);
    public new static Result<T> Failure(Error error) => new(false, default, error);
    public new static Result<T> Failure(string message) => new(false, default, Error.Failure(message));
    public new static Result<T> Failure(string code, string message) => new(false, default, new Error(code, message));
}
