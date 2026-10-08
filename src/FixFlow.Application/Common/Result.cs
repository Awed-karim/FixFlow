namespace FixFlow.Application.Common;

public enum ErrorType
{
    None = 0,
    Validation = 1,
    NotFound = 2,
    Forbidden = 3,
    Conflict = 4
}

public class Result
{
    public bool Succeeded { get; init; }
    public string? Error { get; init; }
    public ErrorType ErrorType { get; init; }

    public static Result Ok() => new() { Succeeded = true };

    public static Result Fail(string error, ErrorType type = ErrorType.Validation) =>
        new() { Succeeded = false, Error = error, ErrorType = type };
}

public class Result<T> : Result
{
    public T? Data { get; init; }

    public static Result<T> Ok(T data) => new() { Succeeded = true, Data = data };

    public new static Result<T> Fail(string error, ErrorType type = ErrorType.Validation) =>
        new() { Succeeded = false, Error = error, ErrorType = type };
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}