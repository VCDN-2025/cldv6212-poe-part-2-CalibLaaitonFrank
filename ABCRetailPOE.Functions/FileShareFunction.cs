using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace AzureFunctionPOE
{
	public class FileShareFunction
	{
		private readonly ShareServiceClient _shareServiceClient;
		private readonly ILogger<FileShareFunction> _logger;

		//MVC constants
		private const string ShareName = "contracts";
		private const string DirectoryName = "uploaded";

		public FileShareFunction(ShareServiceClient shareServiceClient, ILoggerFactory loggerFactory)
		{
			_shareServiceClient = shareServiceClient ?? throw new ArgumentNullException(nameof(shareServiceClient));
			_logger = loggerFactory?.CreateLogger<FileShareFunction>() ?? throw new ArgumentNullException(nameof(loggerFactory));
		}

		[Function("UploadFile")]
		public async Task<HttpResponseData> UploadFile(
			[HttpTrigger(AuthorizationLevel.Function, "post", Route = "files/upload")] HttpRequestData req,
			FunctionContext ctx)
		{
			var logger = ctx.GetLogger("FileShareFunction.UploadFile");
			try
			{
				//Try header or query filename
				string? fileName = null;
				if (req.Headers.TryGetValues("X-File-Name", out var hvals)) fileName = hvals.FirstOrDefault();
				var q = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
				if (string.IsNullOrWhiteSpace(fileName)) fileName = q["fileName"];

				//Check Content-Type
				req.Headers.TryGetValues("Content-Type", out var ctVals);
				var contentType = ctVals != null ? string.Join(";", ctVals) : string.Empty;

				MemoryStream? fileStreamFromMultipart = null;
				if (!string.IsNullOrWhiteSpace(contentType) &&
					contentType.IndexOf("multipart/form-data", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					var mediaType = MediaTypeHeaderValue.Parse(contentType);
					var boundary = HeaderUtilities.RemoveQuotes(mediaType.Boundary).Value;
					if (string.IsNullOrWhiteSpace(boundary))
					{
						var bad = req.CreateResponse(HttpStatusCode.BadRequest);
						await bad.WriteStringAsync("Missing multipart boundary.");
						return bad;
					}

					var reader = new MultipartReader(boundary, req.Body);
					MultipartSection? section;
					while ((section = await reader.ReadNextSectionAsync()) != null)
					{
						var cd = section.GetContentDispositionHeader();
						if (cd == null) continue;
						if (cd.IsFileDisposition())
						{
							// get filename 
							if (string.IsNullOrWhiteSpace(fileName))
								fileName = cd.FileName.HasValue ? cd.FileName.Value : cd.FileNameStar.Value;

							fileStreamFromMultipart = new MemoryStream();
							await section.Body.CopyToAsync(fileStreamFromMultipart);
							fileStreamFromMultipart.Position = 0;
							break;
						}
					}

					if (fileStreamFromMultipart == null)
					{
						var bad = req.CreateResponse(HttpStatusCode.BadRequest);
						await bad.WriteStringAsync("No file found in multipart/form-data parts.");
						return bad;
					}
				}

				// If still no filename
				if (string.IsNullOrWhiteSpace(fileName))
					fileName = $"file-{Guid.NewGuid():N}";

				// Get content stream
				Stream fileContentStream;
				if (fileStreamFromMultipart != null)
					fileContentStream = fileStreamFromMultipart;
				else
				{
					var ms = new MemoryStream();
					await req.Body.CopyToAsync(ms);
					if (ms.Length == 0)
					{
						var bad = req.CreateResponse(HttpStatusCode.BadRequest);
						await bad.WriteStringAsync("Request body is empty.");
						return bad;
					}
					ms.Position = 0;
					fileContentStream = ms;
				}

				// Check if share and directory exist
				var shareClient = _shareServiceClient.GetShareClient(ShareName);
				await shareClient.CreateIfNotExistsAsync();
				var dirClient = shareClient.GetDirectoryClient(DirectoryName);
				await dirClient.CreateIfNotExistsAsync();

				var fileClient = dirClient.GetFileClient(fileName);
				await fileClient.CreateAsync(fileContentStream.Length);
				fileContentStream.Position = 0;
				await fileClient.UploadRangeAsync(new HttpRange(0, fileContentStream.Length), fileContentStream);

				logger.LogInformation("Uploaded file '{FileName}' to {Share}/{Dir}", fileName, ShareName, DirectoryName);

				var resp = req.CreateResponse(HttpStatusCode.OK);
				resp.Headers.Add("Content-Type", "application/json");
				await resp.WriteStringAsync(System.Text.Json.JsonSerializer.Serialize(new { FileName = fileName, Message = "Uploaded" }));
				return resp;
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Upload failed");
				var err = req.CreateResponse(HttpStatusCode.InternalServerError);
				await err.WriteStringAsync($"Error: {ex.Message}");
				return err;
			}
		}

		[Function("ListFiles")]
		public async Task<HttpResponseData> ListFiles([HttpTrigger(AuthorizationLevel.Function, "get", Route = "files/list")] HttpRequestData req, FunctionContext ctx)
		{
			var logger = ctx.GetLogger("FileShareFunction.ListFiles");
			try
			{
				var shareClient = _shareServiceClient.GetShareClient(ShareName);
				var dirClient = shareClient.GetDirectoryClient(DirectoryName);

				var files = new List<string>();
				await foreach (ShareFileItem item in dirClient.GetFilesAndDirectoriesAsync())
					if (!item.IsDirectory) files.Add(item.Name);

				var resp = req.CreateResponse(HttpStatusCode.OK);
				resp.Headers.Add("Content-Type", "application/json");
				await resp.WriteStringAsync(System.Text.Json.JsonSerializer.Serialize(files));
				return resp;
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "List failed");
				var err = req.CreateResponse(HttpStatusCode.InternalServerError);
				await err.WriteStringAsync($"Error: {ex.Message}");
				return err;
			}
		}
	}

	static class MultipartSectionExtensions
	{
		public static ContentDispositionHeaderValue? GetContentDispositionHeader(this MultipartSection section)
		{
			if (ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var cd))
				return cd;
			return null;
		}
	}
}
