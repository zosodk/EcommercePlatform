using System.Threading.Tasks;

namespace EcommercePlatform.Application.Interfaces.Services;

public interface ICloudStorageService
{
    Task<string?> GeneratePresignedUploadUrlAsync(string bucketName, string objectKey, int expirationInSeconds = 3600);
    // Potentially: Task DeleteObjectAsync(string bucketName, string objectKey);
}