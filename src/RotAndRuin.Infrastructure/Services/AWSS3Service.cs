using Amazon.S3;
using Amazon.S3.Transfer;
using RotAndRuin.Application.Interfaces;
using System;

namespace RotAndRuin.Infrastructure.Services
{
    public class AwsS3Service : ICloudStorageService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;

        public AwsS3Service()
        {
            // Read AWS credentials and settings from environment variables
            string accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY")
                ?? throw new ArgumentNullException("AWS_ACCESS_KEY environment variable is not set.");
            string secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_KEY")
                ?? throw new ArgumentNullException("AWS_SECRET_KEY environment variable is not set.");
            string region = Environment.GetEnvironmentVariable("AWS_REGION")
                ?? throw new ArgumentNullException("AWS_REGION environment variable is not set.");
            _bucketName = Environment.GetEnvironmentVariable("AWS_BUCKET_NAME")
                ?? throw new ArgumentNullException("AWS_BUCKET_NAME environment variable is not set.");

            // Initialize the S3 client with credentials and region
            _s3Client = new AmazonS3Client(
                accessKey,
                secretKey,
                Amazon.RegionEndpoint.GetBySystemName(region));

            // Log initialization details (avoid logging actual credentials for security)
            Console.WriteLine($"S3 Client initialized with:");
            Console.WriteLine($"  AccessKey: {(string.IsNullOrEmpty(accessKey) ? "NOT SET" : "SET")}");
            Console.WriteLine($"  SecretKey: {(string.IsNullOrEmpty(secretKey) ? "NOT SET" : "SET")}");
            Console.WriteLine($"  Region: {region}");
            Console.WriteLine($"  BucketName: {_bucketName}");
            Console.WriteLine($"S3 Client initialized for Region: {region}, Bucket: {_bucketName}");
        }

        public AwsS3Service(IAmazonS3 s3Client, string bucketName)
        {
            ArgumentNullException.ThrowIfNull(s3Client, nameof(s3Client));
            ArgumentNullException.ThrowIfNull(bucketName, nameof(bucketName));

            _s3Client = s3Client;
            _bucketName = bucketName;
        }

        private string GetS3Url(string fileName)
        {
            ArgumentNullException.ThrowIfNull(fileName, nameof(fileName));

            // Ensure the bucket name is properly escaped
            var escapedBucketName = Uri.EscapeDataString(_bucketName!);
            if (string.IsNullOrEmpty(escapedBucketName))
                throw new ArgumentException("Failed to escape bucket name", nameof(fileName));
            
            var escapedFileName = Uri.EscapeDataString(fileName);
            var region = _s3Client.Config.RegionEndpoint?.SystemName ?? "us-east-1";

            // Special handling for us-east-1: can use s3.amazonaws.com, but including region for consistency
            var host = region == "us-east-1" 
                ? $"{escapedBucketName}.s3.amazonaws.com" 
                : $"{escapedBucketName}.s3.{region}.amazonaws.com";

            var uriBuilder = new UriBuilder
            {
                Scheme = "https",
                Host = host,
                Path = $"/{escapedFileName}"
            };
            
            return uriBuilder.ToString();
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = fileStream,
                BucketName = _bucketName,
                Key = fileName,
                ContentType = contentType,
            };

            var fileTransferUtility = new TransferUtility(_s3Client);
            await fileTransferUtility.UploadAsync(uploadRequest);

            return GetS3Url(fileName);
        }
    }
}