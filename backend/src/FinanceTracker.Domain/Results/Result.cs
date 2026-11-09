namespace FinanceTracker.Domain.Results;

public readonly struct ErrorResult
{
    public string Message { get; }
    public int StatusCode { get; }

    public ErrorResult(string message, int statusCode = 400)
    {
        Message = message;
        StatusCode = statusCode;
    }

    public static implicit operator string(ErrorResult error) => error.Message;
    public override string ToString() => Message;
}

public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public int StatusCode { get; }

    protected Result(bool isSuccess, string? error, int statusCode = 200)
    {
        IsSuccess = isSuccess;
        Error = error;
        StatusCode = statusCode;
    }

    public static Result Success(int statusCode = 200) => new(true, null, statusCode);
    public static Result Failure(string error, int statusCode = 400) => new(false, error, statusCode);
    public static Result Failure(ErrorResult error) => new(false, error.Message, error.StatusCode);

    public static implicit operator Result(ErrorResult error) => Failure(error);
}

public class Result<T> : Result
{
    public T? Value { get; }

    protected Result(bool isSuccess, T? value, string? error, int statusCode = 200)
        : base(isSuccess, error, statusCode)
    {
        Value = value;
    }

    public static Result<T> Success(T value, int statusCode = 200) => new(true, value, null, statusCode);
    public static new Result<T> Failure(string error, int statusCode = 400) => new(false, default, error, statusCode);
    public static new Result<T> Failure(ErrorResult error) => new(false, default, error.Message, error.StatusCode);

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(ErrorResult error) => Failure(error);
}
