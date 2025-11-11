namespace ExchangeRateSystem.Core.Models;

public class ApiResult<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;

    public static ApiResult<T> Success(T data, string source)
    {
        return new ApiResult<T>
        {
            IsSuccess = true,
            Data = data,
            Source = source
        };
    }

    public static ApiResult<T> Failure(string errorMessage, string source)
    {
        return new ApiResult<T>
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Source = source
        };
    }
}
