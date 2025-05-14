using Amazon.S3;
using Amazon.S3.Model;
using EcommercePlatform.Application.Interfaces.Services;
using Microsoft.Extensions.Logging; // For logging
using System;
using System.Threading.Tasks;

    namespace EcommercePlatform.Infrastructure.CloudStorage;

    public class S3StorageService : ICloudStorageService
    {
        private readonly IAmazonS3 _s3Client; // Injected via DI
        private readonly ILogger<S3StorageService> _logger;

        public S3StorageService(IAmazonS3 s3Client, ILogger<S3StorageService> logger)
        {
            _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string?> GeneratePresignedUploadUrlAsync(string bucketName, string objectKey, int expirationInSeconds = 3600)
        {
            if (string.IsNullOrWhiteSpace(bucketName))
            {
                _logger.LogError("Bucket name cannot be null or whitespace for generating pre-signed URL.");
                throw new ArgumentNullException(nameof(bucketName));
            }
            if (string.IsNullOrWhiteSpace(objectKey))
            {
                _logger.LogError("Object key cannot be null or whitespace for generating pre-signed URL.");
                throw new ArgumentNullException(nameof(objectKey));
            }

            var request = new GetPreSignedUrlRequest
            {
                BucketName = bucketName,
                Key = objectKey,
                Verb = HttpVerb.PUT,
                Expires = DateTime.UtcNow.AddSeconds(expirationInSeconds)
            };

            try
            {
                string url = _s3Client.GetPreSignedURL(request); // This is synchronous in the SDK
                _logger.LogInformation("Successfully generated pre-signed URL for object key: {ObjectKey} in bucket: {BucketName}", objectKey, bucketName);
                return await Task.FromResult(url); // Wrap in Task.FromResult for async interface
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, "AmazonS3Exception generating pre-signed URL for key {ObjectKey} in bucket {BucketName}", objectKey, bucketName);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Generic exception generating pre-signed URL for key {ObjectKey} in bucket {BucketName}", objectKey, bucketName);
                return null;
            }
        }
    }