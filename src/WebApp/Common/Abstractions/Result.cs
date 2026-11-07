

namespace BeasiswaIn.Web.Common.Abstractions;

public class Result
{
    protected Result(bool isSuccess, IReadOnlyList<Error> errors)
    {
        if (isSuccess && errors.Count > 0 ||
            !isSuccess && errors.Count == 0 ||
            !isSuccess && errors.Any(e => e == Error.None))
        {
            throw new ArgumentException("Invalid error", nameof(errors));
        }

        IsSuccess = isSuccess;
        Errors = errors;
    }

    internal bool IsSuccess { get; }

    internal bool IsFailure => !IsSuccess;

    internal IReadOnlyList<Error> Errors { get; }

    internal Error Error => Errors.Count > 0 ? Errors[0] : Error.None;

    internal static Result Success => new(true, []);

    internal static Result Failure(Error error) => new(false, [error]);

    internal static Result Failure(IEnumerable<Error> errors) => new(false, errors.ToList());
}

public class Result<T> : Result
{
    public T? Value { get; }

    private Result(T? value, bool isSuccess, IReadOnlyList<Error> errors)
        : base(isSuccess, errors)
    {
        Value = value;
    }

    public static new Result<T> Success(T value) =>
        new(value, true, []);

    public static new Result<T> Failure(Error error) =>
        new(default, false, [error]);

    public static new Result<T> Failure(IEnumerable<Error> errors) =>
        new(default, false, errors.ToList());
}
