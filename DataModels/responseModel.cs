public class ApiResponse<T>
{
    public bool Success { get; set; }
    public int Code { get; set; }
    public string Message { get; set; }
    public T? Data { get; set; }
    public object? Details { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "OK") =>
        new ApiResponse<T> { Success = true, Code = 200, Message = message, Data = data };

    public static ApiResponse<T> Fail(int code, string message, object? details = null) =>
        new ApiResponse<T> { Success = false, Code = code, Message = message, Details = details };
}
