using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.IdentityModel.Tokens;
using System.Net;

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

    public async Task<string> SaveFileAsync(byte[] uploadFileStream, string bucketName, string fileName)
    {
        Stream fileStream = new MemoryStream(uploadFileStream);

        PutObjectRequest request = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = fileName,
            InputStream = fileStream
        };

        PutObjectResponse response = await _client.PutObjectAsync(request);

        _logging.LogInformation($"File {fileName} uploaded to S3 bucket {bucketName}");

        string fileUri = GetFileUri(bucketName, fileName);

        return fileUri;
    }

    private string GetFileUri(string container, string filePath)
    {
        return $"s3://{container}/{filePath}";
    }

    public string GeneratePresignedURL(string bucketName, string objectKey, double duration)
    {
        string urlString = string.Empty;
        try
        {
            var request = new GetPreSignedUrlRequest()
            {
                BucketName = bucketName,
                Key = objectKey,
                Verb = HttpVerb.PUT,
                Expires = DateTime.UtcNow.AddHours(duration),
            };
            urlString = _client.GetPreSignedURL(request);
        }
        catch (AmazonS3Exception ex)
        {
            Console.WriteLine($"Error:'{ex.Message}'");
        }

        return urlString;
    }

    public bool UploadObject(string filePath, string url)
    {
        HttpWebRequest httpRequest = WebRequest.Create(url) as HttpWebRequest;
        httpRequest.Method = "PUT";
        using (Stream dataStream = httpRequest.GetRequestStream())
        {
            var buffer = new byte[8000];
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                int bytesRead = 0;
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    dataStream.Write(buffer, 0, bytesRead);
                }
            }
        }

        HttpWebResponse response = httpRequest.GetResponse() as HttpWebResponse;
        return response.StatusCode == HttpStatusCode.OK;
    }
}
