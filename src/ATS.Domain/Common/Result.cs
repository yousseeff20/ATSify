namespace ATS.Domain.Common;

public class Result<T>
{
    public T? Value { get; }
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }

    protected Result(T? value, bool isSuccess, string? errorMessage)
    {
        Value = value;
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }

    public static Result<T> Success(T value) => new(value, true, null);
    public static Result<T> Failure(string errorMessage) => new(default, false, errorMessage);
}

public class Result : Result<string>
{
    protected Result(bool isSuccess, string? errorMessage) 
        : base(null, isSuccess, errorMessage)
    {
    }

    public static Result Success() => new(true, null);
    public static new Result Failure(string errorMessage) => new(false, errorMessage);
}
