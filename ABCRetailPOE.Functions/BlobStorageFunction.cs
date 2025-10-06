using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Functions
{
    public class BlobFunction
    {
        private readonly BlobContainerClient _containerClient;

        public BlobFunction()
        {
            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            var blobServiceClient = new BlobServiceClient(connectionString);
            _containerClient = blobServiceClient.GetBlobContainerClient("uploads");
            _containerClient.CreateIfNotExists();
        }

        [Function("UploadBlob")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("UploadBlob");

            string blobName = $"file-{Guid.NewGuid()}.txt";
            var blobClient = _containerClient.GetBlobClient(blobName);

            using var stream = new MemoryStream();
            await req.Body.CopyToAsync(stream);
            stream.Position = 0;

            await blobClient.UploadAsync(stream, overwrite: true);
            logger.LogInformation($"Blob {blobName} uploaded.");

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteStringAsync($"Blob uploaded: {blobName}");
            return response;
        }
    }
}