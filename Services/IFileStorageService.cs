namespace SciSubmit.Services
{
    public interface IFileStorageService
    {
        /// <summary>
        /// Upload file to storage
        /// </summary>
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string folder = "uploads");

        /// <summary>
        /// Get file URL from storage
        /// </summary>
        Task<string> GetFileUrlAsync(string filePath);

        /// <summary>
        /// Delete file from storage
        /// </summary>
        Task<bool> DeleteFileAsync(string filePath);

        /// <summary>
        /// Check if file exists
        /// </summary>
        Task<bool> FileExistsAsync(string filePath);
    }
}











