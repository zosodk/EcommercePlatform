using Amazon.S3;
using Amazon.S3.Model;
using EcommercePlatform.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace EcommercePlatform.Infrastructure.CloudStorage;

public class S3StorageService : ICloudStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly ILogger<S3StorageService> _logger;

    public S3StorageService(IAmazonS3 s3Client, ILogger<S3StorageService> logger)
    {
        _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string?> GeneratePresignedUploadUrlAsync(string bucketName, string objectKey, int expirationInSeconds = 3600)
    {
        if (string.IsNullOrWhiteSpace(bucketName)) throw new ArgumentNullException(nameof(bucketName));
        if (string.IsNullOrWhiteSpace(objectKey)) throw new ArgumentNullException(nameof(objectKey));
        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucketName, Key = objectKey, Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddSeconds(expirationInSeconds)
        };
        try
        {
            string url = _s3Client.GetPreSignedURL(request); // Synchronous SDK method
            _logger.LogInformation("Generated pre-signed URL for {ObjectKey} in bucket {BucketName}", objectKey, bucketName);
            return await Task.FromResult(url); // Wrap for async interface
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating pre-signed URL for {ObjectKey} in bucket {BucketName}", objectKey, bucketName);
            return null;
        }
    }
}