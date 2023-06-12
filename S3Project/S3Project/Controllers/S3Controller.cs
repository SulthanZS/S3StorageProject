using Amazon.S3;
using Microsoft.AspNetCore.Mvc;
using S3Project.Business;

namespace S3Project.Controllers
{
    public class S3Controller : Controller
    {
        private readonly S3FileManager fileManager;
        private readonly IConfiguration configuration;
        private readonly AmazonS3Storage amazonS3Storage;

        public S3Controller(S3FileManager fileManager, IConfiguration configuration, AmazonS3Storage amazonS3Storage)
        {
            this.fileManager = fileManager;
            this.configuration = configuration;
            this.amazonS3Storage= amazonS3Storage;
        }

        [HttpGet]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [Route("")]
        public String Index()
        {
            var s3Config = new AmazonS3Config
            {
                RegionEndpoint = Amazon.RegionEndpoint.APSoutheast1 // Replace with your desired region
            };

            var s3Client = new AmazonS3Client(s3Config);
            string signatureVersion = s3Client.Config.SignatureVersion;

           return ($"Signature Version: {signatureVersion}");
        }

        [HttpPost("upload")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult Upload(IFormFile file, string bucketName)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file selected for upload.");
            }

            try
            {
                string s3Key = Guid.NewGuid().ToString(); // Generate a unique key for the file

                using (var memoryStream = new MemoryStream())
                {
                    file.CopyTo(memoryStream);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    fileManager.UploadFile(bucketName, memoryStream, s3Key);
                }

                return Ok($"File uploaded successfully to S3 bucket: {bucketName}, Key: {s3Key}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error uploading file: {ex.Message}");
            }
        }

        [HttpPost("upload2")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Upload2(string bucketName, string objectName, string path)
        {
            if (path == null || path.Length == 0)
            {
                return BadRequest("No file selected for upload.");
            }

            try
            {
                string s3Key = Guid.NewGuid().ToString(); // Generate a unique key for the file

                await fileManager.UploadFileAsync(bucketName, objectName, path);

                return Ok($"File uploaded successfully to S3 bucket: {bucketName}, Key: {s3Key}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error uploading file: {ex.Message}");
            }
        }

        [HttpPost("upload3")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Upload3(string bucketName, IFormFile file)
        {
            using (var memoryStream = new MemoryStream())
            {
                file.CopyTo(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);
                string uri = await amazonS3Storage.SaveFileAsync(memoryStream.ToArray(), bucketName, file.FileName);
                
                return new OkObjectResult(uri);
            }

           
        }

        [HttpPut("upload4")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public void Upload4(string bucketName, IFormFile file, string filePath)
        {
         
            filePath = filePath + "\\" + file.FileName;

            // Specify how long the signed URL will be valid in hours.
            double timeoutDuration = 12;

            // If the AWS Region defined for your default user is different
            // from the Region where your Amazon S3 bucket is located,
            // pass the Region name to the Amazon S3 client object's constructor.
            // For example: RegionEndpoint.USWest2.
            IAmazonS3 client = new AmazonS3Client();

            var url = amazonS3Storage.GeneratePresignedURL(bucketName, file.FileName, timeoutDuration);
            var success = amazonS3Storage.UploadObject(filePath, url);

            if (success)
            {
                Console.WriteLine("Upload succeeded.");
            }
            else
            {
                Console.WriteLine("Upload failed.");
            }


        }



        [HttpGet("download/{s3Key}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult Download(string s3Key, string bucketName, string destinationPath)
        {
            try
            {
                fileManager.DownloadFile(bucketName, s3Key, destinationPath);

                return Ok($"File downloaded successfully from S3 bucket: {bucketName}, Key: {s3Key}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error downloading file: {ex.Message}");
            }
        }

        [HttpGet("download")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Download2(string objectName, string bucketName, string destinationPath)
        {
            try
            {
                await fileManager.DownloadObjectFromBucketAsync(bucketName, objectName, destinationPath);

                return Ok($"File downloaded successfully from S3 bucket: {bucketName}, Object Name: {objectName}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error downloading file: {ex.Message}");
            }
        }
    }
}
