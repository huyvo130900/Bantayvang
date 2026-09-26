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

                // BUG FIX (path traversal): subFolder comes straight from an attacker-controlled
                // query string (UploadController.UploadImage's [FromQuery] string folder, reachable
                // by any ManagementOnly caller including DeptManager) with no sanitization. The same
                // Path.Combine-discards-the-base-on-a-rooted-segment behavior already fixed for
                // DeleteFileAsync below applies here too - e.g. folder="../../../somewhere" (or an
                // absolute path) would let an uploaded file land outside wwwroot/uploads entirely.
                // Sanitize the same way: reject rooted/traversal segments and verify containment.
                var uploadsRoot = Path.GetFullPath(Path.Combine(_env.ContentRootPath, "wwwroot", "uploads"));
                var safeSubFolder = string.IsNullOrWhiteSpace(subFolder) || Path.IsPathRooted(subFolder) || subFolder.Contains("..")
                    ? "questions"
                    : subFolder;
                var uploadsFolder = Path.GetFullPath(Path.Combine(uploadsRoot, safeSubFolder));
                if (!uploadsFolder.StartsWith(uploadsRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(uploadsFolder, uploadsRoot, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Blocked attempt to upload outside the uploads directory: {SubFolder}", subFolder);
                    safeSubFolder = "questions";
                    uploadsFolder = Path.Combine(uploadsRoot, safeSubFolder);
                }
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
                var fileUrl = $"{baseUrl}/uploads/{safeSubFolder}/{safeFileName}";

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

                // BUG FIX (arbitrary file deletion): this used to build the target path as
                // Path.Combine(ContentRootPath, "wwwroot", new Uri(fileUrl).AbsolutePath.TrimStart('/'))
                // with no further checks. Any ManagementOnly caller (Admin OR DeptManager) could pass
                // fileUrl="file:///C:/anything/anywhere.ext" - Uri.AbsolutePath then returns
                // "/C:/anything/anywhere.ext", and Path.Combine's documented behavior is to DISCARD
                // every earlier segment once it sees one that looks rooted (a drive letter), so the
                // "safe" base path was silently dropped entirely and File.Delete ran on the raw
                // attacker-supplied absolute path - confirmed live: this deleted an arbitrary test
                // file placed completely outside wwwroot. Every real upload URL this app ever hands
                // out looks like "{scheme}://{host}/uploads/{subFolder}/{guid}.{ext}" (see
                // UploadImageAsync above), so only ever resolve within wwwroot/uploads, and verify
                // the final resolved path is still inside that directory before deleting anything.
                var uploadsRoot = Path.GetFullPath(Path.Combine(_env.ContentRootPath, "wwwroot", "uploads"));

                string relativePath;
                if (Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                {
                    relativePath = uri.AbsolutePath.TrimStart('/');
                }
                else
                {
                    // Not an http(s) URL (e.g. a bare "/uploads/questions/x.png" path) - treat the
                    // whole string as a relative path candidate; still fully validated below.
                    relativePath = fileUrl.TrimStart('/');
                }

                if (!relativePath.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
                    return false;
                relativePath = relativePath.Substring("uploads/".Length);

                if (Path.IsPathRooted(relativePath) || relativePath.Contains(".."))
                    return false;

                var filePath = Path.GetFullPath(Path.Combine(uploadsRoot, relativePath));

                // Defense in depth: the resolved path must still be inside uploadsRoot.
                if (!filePath.StartsWith(uploadsRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Blocked attempt to delete a file outside the uploads directory: {FileUrl}", fileUrl);
                    return false;
                }

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

            using var stream = file.OpenReadStream();
            using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
            var signatures = FileSignatures[extension];
            var headerBytes = reader.ReadBytes(signatures.Max(s => s.Length));

            // Reset stream position
            stream.Position = 0;

            return signatures.Any(signature =>
                headerBytes.Take(signature.Length).SequenceEqual(signature));
        }
    }
}