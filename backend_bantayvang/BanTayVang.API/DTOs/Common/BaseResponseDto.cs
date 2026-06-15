namespace BanTayVang.API.DTOs.Common
{
    public class BaseResponseDto<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new();

        public BaseResponseDto()
        {
        }

        public BaseResponseDto(bool success, string message, T? data = default, List<string>? errors = null)
        {
            Success = success;
            Message = message;
            Data = data;
            Errors = errors ?? new List<string>();
        }

        public static BaseResponseDto<T> SuccessResult(T data, string message = "Operation successful")
        {
            return new BaseResponseDto<T>(true, message, data);
        }

        public static BaseResponseDto<T> SuccessResult(string message = "Operation successful")
        {
            return new BaseResponseDto<T>(true, message);
        }

        public static BaseResponseDto<T> FailureResult(string message, List<string>? errors = null)
        {
            return new BaseResponseDto<T>(false, message, default, errors);
        }
    }

    public class BaseResponseDto : BaseResponseDto<object>
    {
        public BaseResponseDto() : base()
        {
        }

        public BaseResponseDto(bool success, string message, object? data = null, List<string>? errors = null) 
            : base(success, message, data, errors)
        {
        }

        public static new BaseResponseDto SuccessResult(string message = "Operation successful")
        {
            return new BaseResponseDto(true, message);
        }

        public static BaseResponseDto SuccessResult(object data, string message = "Operation successful")
        {
            return new BaseResponseDto(true, message, data);
        }

        public static new BaseResponseDto FailureResult(string message, List<string>? errors = null)
        {
            return new BaseResponseDto(false, message, null, errors);
        }
    }
}