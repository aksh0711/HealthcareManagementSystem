namespace HealthcareManagementSystem.Services
{
    public class FileUploadService : IFileUploadService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<FileUploadService> _logger;
        private readonly string[] _allowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
        private readonly string[] _allowedDocumentExtensions = { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };
        private readonly long _maxFileSize = 5 * 1024 * 1024; // 5MB

        public FileUploadService(IWebHostEnvironment webHostEnvironment, ILogger<FileUploadService> logger)
        {
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        public async Task<string> UploadFileAsync(IFormFile file, string subfolder)
        {
            try
            {
                if (file == null || file.Length == 0)
                    throw new ArgumentException("File is empty or null");

                if (file.Length > _maxFileSize)
                    throw new ArgumentException($"File size exceeds the maximum allowed size of {_maxFileSize / (1024 * 1024)}MB");

                // Create uploads directory if it doesn't exist
                var uploadsPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", subfolder);
                Directory.CreateDirectory(uploadsPath);

                // Generate unique filename
                var fileExtension = GetFileExtension(file.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsPath, uniqueFileName);

                // Save file
                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);

                var relativePath = Path.Combine("uploads", subfolder, uniqueFileName).Replace("\\", "/");
                
                _logger.LogInformation($"File uploaded successfully: {relativePath}");
                return relativePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file");
                throw;
            }
        }

        public void DeleteFile(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                    return;

                var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, filePath);
                
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    _logger.LogInformation($"File deleted successfully: {filePath}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to delete file: {filePath}");
                // Don't throw here as file deletion is not critical
            }
        }

        public bool IsValidImageFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return false;

            var extension = GetFileExtension(file.FileName).ToLowerInvariant();
            return _allowedImageExtensions.Contains(extension);
        }

        public bool IsValidDocumentFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return false;

            var extension = GetFileExtension(file.FileName).ToLowerInvariant();
            return _allowedDocumentExtensions.Contains(extension);
        }

        public string GetFileExtension(string fileName)
        {
            return Path.GetExtension(fileName)?.ToLowerInvariant() ?? string.Empty;
        }
    }
}
