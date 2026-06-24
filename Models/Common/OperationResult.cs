namespace ITQS.SupportOperationsCenter.Models.Common;

public sealed class OperationResult<T>
{
    public bool Success { get; private set; }
    public string? ErrorMessage { get; private set; }
    public T? Data { get; private set; }

    public static OperationResult<T> Ok(T data)
    {
        return new OperationResult<T>
        {
            Success = true,
            Data = data
        };
    }

    public static OperationResult<T> Fail(string errorMessage)
    {
        return new OperationResult<T>
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}
