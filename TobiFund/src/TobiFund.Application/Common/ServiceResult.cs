namespace TobiFund.Application.Common;

/// <summary>نتیجه اجرای عملیات سرویس</summary>
public class ServiceResult
{
    public bool Success { get; protected set; }
    public string Message { get; protected set; } = string.Empty;

    public static ServiceResult Ok(string message = "عملیات با موفقیت انجام شد.") =>
        new() { Success = true, Message = message };

    public static ServiceResult Fail(string message) =>
        new() { Success = false, Message = message };
}

/// <summary>نتیجه اجرای عملیات سرویس به همراه داده</summary>
public class ServiceResult<T> : ServiceResult
{
    public T? Data { get; private set; }

    public static ServiceResult<T> Ok(T data, string message = "عملیات با موفقیت انجام شد.") =>
        new() { Success = true, Message = message, Data = data };

    public static new ServiceResult<T> Fail(string message) =>
        new() { Success = false, Message = message };
}
