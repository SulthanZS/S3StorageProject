using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.IdentityModel.Tokens;

namespace S3Project.Business;

public class AmazonS3Storage
{
    private ILogger<AmazonS3Storage> _logging;
    private AmazonS3Client _client;

    public AmazonS3Storage(
        ILogger<AmazonS3Storage> logging,
        IConfiguration config)
    {
        _logging = logging;

        _client = new AmazonS3Client(config.GetValue<string>("S3:aws_access_key_id"), config.GetValue<string>("S3:aws_secret_access_key"), RegionEndpoint.GetBySystemName(config.GetValue<string>("S3:region")));
    }

    public int CountFileOnFolder(string container, string folder)
    {
        int count = 0;

        ListObjectsV2Request request = new ListObjectsV2Request
        {
            BucketName = container,
            Prefix = folder
        };

        do
        {
            ListObjectsV2Response response = _client.ListObjectsV2Async(request).Result;

            count += response.S3Objects.Count;

            request.ContinuationToken = response.NextContinuationToken;
        }
        while (request.ContinuationToken != null);

        return count;
    }

    public void DeleteFile(string uri)
    {
        List<string> formattedStr = uri.Split("/").Where(s => !s.IsNullOrEmpty()).ToList();
        string bucketName = formattedStr[1];
        string key = formattedStr[2];

        DeleteFile(bucketName, key);
    }

    public void DeleteFile(string container, string fileName)
    {
        DeleteObjectRequest request = new DeleteObjectRequest
        {
            BucketName = container,
            Key = fileName
        };

        DeleteObjectResponse response = _client.DeleteObjectAsync(request).Result;
        _logging.LogInformation("File deleted successfully");
    }

    public void DeleteFolder(string container, string folder)
    {
        ListObjectsV2Request listRequest = new ListObjectsV2Request
        {
            BucketName = container,
            Prefix = folder
        };

        ListObjectsV2Response listResponse = _client.ListObjectsV2Async(listRequest).Result;

        DeleteObjectsRequest deleteRequest = new DeleteObjectsRequest
        {
            BucketName = container,
            Objects = new List<KeyVersion>()
        };

        foreach (S3Object obj in listResponse.S3Objects)
        {
            deleteRequest.Objects.Add(new KeyVersion { Key = obj.Key });
        }

        DeleteObjectsResponse deleteResponse = _client.DeleteObjectsAsync(deleteRequest).Result;

        _logging.LogInformation($"Deleted {deleteResponse.DeletedObjects.Count} objects");
    }

    public async Task<string> SaveFileAsync(byte[] uploadFileStream, string container, string fileName)
    {
        Stream fileStream = new MemoryStream(uploadFileStream);

        PutObjectRequest request = new PutObjectRequest
        {
            BucketName = container,
            Key = fileName,
            InputStream = fileStream
        };

        PutObjectResponse response = await _client.PutObjectAsync(request);

        _logging.LogInformation($"File {fileName} uploaded to S3 bucket {container}");

        string fileUri = GetFileUri(container, fileName);

        return fileUri;
    }

    private string GetFileUri(string container, string filePath)
    {
        return $"s3://{container}/{filePath}";
    }
}
