namespace BanTayVang.API.Services.Interfaces
{
    /// <summary>
    /// File upload service for images
    /// OWASP A08: Software and Data Integrity Failures - File validation
    /// </summary>
    public interface IFileUploadService
    {
        /// <summary>
        /// Upload an image file
        /// </summary>
        Task<FileUploadResult> UploadImageAsync(IFormFile file, string subFolder = "questions");

        /// <summary>
        /// Delete an uploaded file
        /// </summary>
        Task<bool> DeleteFileAsync(string fileUrl);

        /// <summary>
        /// Validate file (size, type, content)
        /// </summary>
        FileValidationResult ValidateImageFile(IFormFile file);
    }

    public class FileUploadResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? FileUrl { get; set; }
        public string? FileName { get; set; }
        public long? FileSize { get; set; }
    }

    public class FileValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
    }
}