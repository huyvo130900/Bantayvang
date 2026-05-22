using BanTayVang.API.Services.Interfaces;

namespace BanTayVang.API.Services.Impl
{
    /// <summary>
    /// File upload implementation with OWASP security
    /// </summary>
    public class FileUploadService : IFileUploadService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<FileUploadService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        // OWASP A08: Whitelist allowed file types
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" };
        private readonly string[] _allowedMimeTypes = { "image/jpeg", "image/png", "image/gif", "image/webp", "image/bmp" };
        private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

        // Magic numbers for image validation
        private static readonly Dictionary<string, byte[][]> FileSignatures = new()
        {
            { ".jpg", new[] { new byte[] { 0xFF, 0xD8, 0xFF } } },
            { ".jpeg", new[] { new byte[] { 0xFF, 0xD8, 0xFF } } },
            { ".png", new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47 } } },
            { ".gif", new[] { new byte[] { 0x47, 0x49, 0x46, 0x38 } } },
            { ".webp", new[] { new byte[] { 0x52, 0x49, 0x46, 0x46 } } },
            { ".bmp", new[] { new byte[] { 0x42, 0x4D } } }
        };

        public FileUploadService(
            IWebHostEnvironment env,
            ILogger<FileUploadService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _env = env;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public FileValidationResult ValidateImageFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return new FileValidationResult { IsValid = false, ErrorMessage = "File trống" };

            // Check size
            if (file.Length > MaxFileSize)
                return new FileValidationResult { IsValid = false, ErrorMessage = "File quá lớn (tối đa 10MB)" };

            // Check extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
                return new FileValidationResult { IsValid = false, ErrorMessage = "Định dạng file không hỗ trợ. Chỉ chấp nhận: " + string.Join(", ", _allowedExtensions) };

            // Check MIME type
            if (!_allowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
                return new FileValidationResult { IsValid = false, ErrorMessage = "MIME type không hợp lệ" };

            // OWASP A08: Validate file signature (magic numbers) to prevent file type spoofing
            if (!ValidateFileSignature(file, extension))
                return new FileValidationResult { IsValid = false, ErrorMessage = "Nội dung file không khớp với định dạng" };

            return new FileValidationResult { IsValid = true };
        }

        public async Task<FileUploadResult> UploadImageAsync(IFormFile file, string subFolder = "questions")
        {
            try
            {
                var validation = ValidateImageFile(file);
                if (!validation.IsValid)
                    return new FileUploadResult { Success = false, Message = validation.ErrorMessage };

                // Build path
                var uploadsFolder = Path.Combine(_env.ContentRootPath, "wwwroot", "uploads", subFolder);
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                // Generate safe filename
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var safeFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, safeFileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Build URL
                var request = _httpContextAccessor.HttpContext?.Request;
                var baseUrl = request != null ? $"{request.Scheme}://{request.Host}" : "";
                var fileUrl = $"{baseUrl}/uploads/{subFolder}/{safeFileName}";

                _logger.LogInformation("File uploaded: {FileName}, Size: {Size}", safeFileName, file.Length);

                return new FileUploadResult
                {
                    Success = true,
                    Message = "Upload thành công",
                    FileUrl = fileUrl,
                    FileName = safeFileName,
                    FileSize = file.Length
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                return new FileUploadResult { Success = false, Message = "Lỗi upload: " + ex.Message };
            }
        }

        public async Task<bool> DeleteFileAsync(string fileUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(fileUrl)) return false;

                // Extract relative path from URL
                var uri = new Uri(fileUrl);
                var relativePath = uri.AbsolutePath.TrimStart('/');
                var filePath = Path.Combine(_env.ContentRootPath, "wwwroot", relativePath);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogInformation("File deleted: {Path}", filePath);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file");
                return false;
            }
        }

        /// <summary>
        /// Validate file content matches the extension (magic numbers)
        /// OWASP A08 prevention
        /// </summary>
        private bool ValidateFileSignature(IFormFile file, string extension)
        {
            if (!FileSignatures.ContainsKey(extension))
                return false;

            using var reader = new BinaryReader(file.OpenReadStream());
            var signatures = FileSignatures[extension];
            var headerBytes = reader.ReadBytes(signatures.Max(s => s.Length));

            // Reset stream position
            file.OpenReadStream().Position = 0;

            return signatures.Any(signature =>
                headerBytes.Take(signature.Length).SequenceEqual(signature));
        }
    }
}