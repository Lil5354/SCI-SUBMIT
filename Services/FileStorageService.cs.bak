using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Hosting;

namespace SciSubmit.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileStorageService> _logger;
        private readonly IAmazonS3? _s3Client;

        public FileStorageService(
            IConfiguration configuration,
            IWebHostEnvironment environment,
            ILogger<FileStorageService> logger)
        {
            _configuration = configuration;
            _environment = environment;
            _logger = logger;

            // Initialize S3 client if configured
            var awsAccessKey = _configuration["Storage:AWS:AccessKey"];
            var awsSecretKey = _configuration["Storage:AWS:SecretKey"];
            var awsRegion = _configuration["Storage:AWS:Region"];

            if (!string.IsNullOrEmpty(awsAccessKey) && !string.IsNullOrEmpty(awsSecretKey))
            {
                var region = Amazon.RegionEndpoint.GetBySystemName(awsRegion ?? "us-east-1");
                _s3Client = new AmazonS3Client(awsAccessKey, awsSecretKey, region);
            }
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string folder = "uploads")
        {
            try
            {
                var useS3 = _configuration.GetValue<bool>("Storage:UseS3", false);
                var bucketName = _configuration["Storage:AWS:BucketName"];

                if (useS3 && _s3Client != null && !string.IsNullOrEmpty(bucketName))
                {
                    return await UploadToS3Async(fileStream, fileName, folder, bucketName);
                }
                else
                {
                    return await UploadToLocalAsync(fileStream, fileName, folder);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file {FileName}", fileName);
                throw;
            }
        }

        private async Task<string> UploadToS3Async(Stream fileStream, string fileName, string folder, string bucketName)
        {
            var key = $"{folder}/{Guid.NewGuid()}_{fileName}";

            var request = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = key,
                InputStream = fileStream,
                ContentType = GetContentType(fileName),
                CannedACL = S3CannedACL.Private // Or PublicRead if needed
            };

            await _s3Client!.PutObjectAsync(request);

            _logger.LogInformation("File uploaded to S3: {Key}", key);
            return key; // Return S3 key
        }

        private async Task<string> UploadToLocalAsync(Stream fileStream, string fileName, string folder)
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, folder);
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStreamOutput = new FileStream(filePath, FileMode.Create))
            {
                await fileStream.CopyToAsync(fileStreamOutput);
            }

            var relativePath = $"/{folder}/{uniqueFileName}";
            _logger.LogInformation("File uploaded to local storage: {Path}", relativePath);
            return relativePath;
        }

        public async Task<string> GetFileUrlAsync(string filePath)
        {
            try
            {
                var useS3 = _configuration.GetValue<bool>("Storage:UseS3", false);
                var bucketName = _configuration["Storage:AWS:BucketName"];

                if (useS3 && _s3Client != null && !string.IsNullOrEmpty(bucketName))
                {
                    // Generate pre-signed URL (valid for 1 hour)
                    var request = new GetPreSignedUrlRequest
                    {
                        BucketName = bucketName,
                        Key = filePath,
                        Verb = HttpVerb.GET,
                        Expires = DateTime.UtcNow.AddHours(1)
                    };

                    return _s3Client.GetPreSignedURL(request);
                }
                else
                {
                    // Return local URL
                    return filePath;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file URL for {FilePath}", filePath);
                return filePath; // Fallback to original path
            }
        }

        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                var useS3 = _configuration.GetValue<bool>("Storage:UseS3", false);
                var bucketName = _configuration["Storage:AWS:BucketName"];

                if (useS3 && _s3Client != null && !string.IsNullOrEmpty(bucketName))
                {
                    var request = new DeleteObjectRequest
                    {
                        BucketName = bucketName,
                        Key = filePath
                    };

                    await _s3Client.DeleteObjectAsync(request);
                    _logger.LogInformation("File deleted from S3: {Key}", filePath);
                    return true;
                }
                else
                {
                    var fullPath = Path.Combine(_environment.WebRootPath, filePath.TrimStart('/'));
                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                        _logger.LogInformation("File deleted from local storage: {Path}", fullPath);
                        return true;
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file {FilePath}", filePath);
                return false;
            }
        }

        public async Task<bool> FileExistsAsync(string filePath)
        {
            try
            {
                var useS3 = _configuration.GetValue<bool>("Storage:UseS3", false);
                var bucketName = _configuration["Storage:AWS:BucketName"];

                if (useS3 && _s3Client != null && !string.IsNullOrEmpty(bucketName))
                {
                    try
                    {
                        var request = new GetObjectMetadataRequest
                        {
                            BucketName = bucketName,
                            Key = filePath
                        };

                        await _s3Client.GetObjectMetadataAsync(request);
                        return true;
                    }
                    catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        return false;
                    }
                }
                else
                {
                    var fullPath = Path.Combine(_environment.WebRootPath, filePath.TrimStart('/'));
                    return await Task.FromResult(System.IO.File.Exists(fullPath));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking file existence {FilePath}", filePath);
                return false;
            }
        }

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };
        }
    }
}


