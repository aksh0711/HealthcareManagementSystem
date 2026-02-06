namespace HealthcareManagementSystem.Services
{
    public interface IFileUploadService
    {
        Task<string> UploadFileAsync(IFormFile file, string subfolder);
        void DeleteFile(string filePath);
        bool IsValidImageFile(IFormFile file);
        bool IsValidDocumentFile(IFormFile file);
        string GetFileExtension(string fileName);
    }
}
