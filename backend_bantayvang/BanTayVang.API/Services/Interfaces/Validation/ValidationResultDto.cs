namespace BanTayVang.API.Services.Interfaces.Validation
{
    /// <summary>
    /// Validation result DTO with detailed error information
    /// Follows OWASP secure error handling
    /// </summary>
    public class ValidationResultDto
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public Dictionary<string, List<string>> FieldErrors { get; set; } = new();
        public string? ErrorCode { get; set; }

        public static ValidationResultDto Success(string message = "Validation successful")
        {
            return new ValidationResultDto
            {
                IsValid = true,
                Message = message
            };
        }

        public static ValidationResultDto Failure(string message, string? errorCode = null)
        {
            return new ValidationResultDto
            {
                IsValid = false,
                Message = message,
                Errors = new List<string> { message },
                ErrorCode = errorCode
            };
        }

        public static ValidationResultDto Failure(List<string> errors, string? errorCode = null)
        {
            return new ValidationResultDto
            {
                IsValid = false,
                Message = errors.FirstOrDefault() ?? "Validation failed",
                Errors = errors,
                ErrorCode = errorCode
            };
        }
    }
}