namespace ORManagement.Application.DTOs.Shared;

public class ServiceResultDto
{
    public bool Success { get; set; }
    public string? ErrorCode { get; set; }
    public string? Message { get; set; }

    public static ServiceResultDto Ok(string? message = null)
    {
        return new ServiceResultDto
        {
            Success = true,
            Message = message
        };
    }

    public static ServiceResultDto Fail(string errorCode, string message)
    {
        return new ServiceResultDto
        {
            Success = false,
            ErrorCode = errorCode,
            Message = message
        };
    }
}

public class ServiceResultDto<T> : ServiceResultDto
{
    public T? Data { get; set; }

    public static ServiceResultDto<T> Ok(T data, string? message = null)
    {
        return new ServiceResultDto<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    public static new ServiceResultDto<T> Fail(string errorCode, string message)
    {
        return new ServiceResultDto<T>
        {
            Success = false,
            ErrorCode = errorCode,
            Message = message
        };
    }
}