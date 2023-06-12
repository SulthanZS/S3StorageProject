using System;
using System.IO;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;

namespace S3Project.Business
{
    public class S3FileManager
    {
        private IConfiguration _config;

        public S3FileManager(IConfiguration config)
        {
            _config = config;

        }

        public void UploadFile(string bucketName, Stream fileStream, string s3Key)
        {
            try
            {
                var accessKey = _config.GetValue<string>("S3:aws_access_key_id");
                var secretAccessKey = _config.GetValue<string>("S3:aws_secret_access_key");
                var region = _config.GetValue<string>("S3:region");

                var s3Client = new AmazonS3Client(accessKey, secretAccessKey, RegionEndpoint.GetBySystemName(region));
                var fileTransferUtility = new TransferUtility(s3Client);
                fileTransferUtility.Upload(fileStream, bucketName, s3Key);
                Console.WriteLine($"File uploaded successfully to S3 bucket: {bucketName}, Key: {s3Key}");
            }
            catch (AmazonS3Exception ex)
            {
                Console.WriteLine($"Error uploading file to S3: {ex.Message}");
            }
        }

        public void DownloadFile(string bucketName, string s3Key, string destinationPath)
        {
            try
            {
                var s3Client = new AmazonS3Client(_config.GetValue<string>("S3:aws_access_key_id"), _config.GetValue<string>("S3:aws_secret_access_key"), RegionEndpoint.GetBySystemName(_config.GetValue<string>("S3:region")));
                var fileTransferUtility = new TransferUtility(s3Client);
                fileTransferUtility.Download(destinationPath, bucketName, s3Key);
                Console.WriteLine($"File downloaded successfully from S3 bucket: {bucketName}, Key: {s3Key}");
            }
            catch (AmazonS3Exception ex)
            {
                Console.WriteLine($"Error downloading file from S3: {ex.Message}");
            }
        }

        public async Task<bool> UploadFileAsync(string bucketName,string objectName,string filePath)
        {
            var accessKey = _config.GetValue<string>("S3:aws_access_key_id");
            var secretAccessKey = _config.GetValue<string>("S3:aws_secret_access_key");
            var region = _config.GetValue<string>("S3:region");

            var s3Client = new AmazonS3Client(accessKey, secretAccessKey, RegionEndpoint.GetBySystemName(region));

            var request = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = objectName,
                FilePath = filePath,
            };

            var response = await s3Client.PutObjectAsync(request);
            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                Console.WriteLine($"Successfully uploaded {objectName} to {bucketName}.");
                return true;
            }
            else
            {
                Console.WriteLine($"Could not upload {objectName} to {bucketName}.");
                return false;
            }
        }

        public async Task<bool> DownloadObjectFromBucketAsync(string bucketName,string objectName,string filePath)
        {
            try
            {
                var accessKey = _config.GetValue<string>("S3:aws_access_key_id");
                var secretAccessKey = _config.GetValue<string>("S3:aws_secret_access_key");
                var region = _config.GetValue<string>("S3:region");

                var s3Client = new AmazonS3Client(accessKey, secretAccessKey, RegionEndpoint.GetBySystemName(region));
                // Create a GetObject request
                var request = new GetObjectRequest
                {
                    BucketName = bucketName,
                    Key = objectName,
                };

                // Issue request and remember to dispose of the response
                using GetObjectResponse response = await s3Client.GetObjectAsync(request);

            
                // Save object to local file
                await response.WriteResponseStreamToFileAsync($"{filePath}\\{objectName}", true, CancellationToken.None);
                return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
            }
            catch (AmazonS3Exception ex)
            {
                Console.WriteLine($"Error saving {objectName}: {ex.Message}");
                return false;
            }
        }
    }
}
