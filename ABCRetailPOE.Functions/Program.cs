using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Files.Shares;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

var builder = FunctionsApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();
 
builder.ConfigureFunctionsWebApplication();

// Application Insights 
builder.Services
	.AddApplicationInsightsTelemetryWorkerService()
	.ConfigureFunctionsApplicationInsights();

// Read connection string from env 
var storageConn = Environment.GetEnvironmentVariable("StorageConnectionString")
			   ?? Environment.GetEnvironmentVariable("AzureWebJobsStorage")
			   ?? throw new InvalidOperationException("Storage connection string not found. Set 'StorageConnectionString' or 'AzureWebJobsStorage' in Function App settings.");

// Register clients in DI
builder.Services.AddSingleton(_ => new BlobServiceClient(storageConn));
builder.Services.AddSingleton(_ => new TableServiceClient(storageConn));
builder.Services.AddSingleton(_ => new ShareServiceClient(storageConn));
builder.Services.AddSingleton(_ => new QueueServiceClient(storageConn));

// Register your function classes so constructor DI works
builder.Services.AddScoped<AzureFunctionPOE.FileShareFunction>();
builder.Services.AddScoped<AzureFunctionPOE.TableStorageFunction>();

var host = builder.Build();
host.Run();

//Microsoft. 2025. Azure Functions Overview.[Online].Available at: https://learn.microsoft.com/en-us/azure/azure-functions/functions-overview [Accessed: 6 October 2025].

//Microsoft. 2025. Quickstart: Create a C# function in Azure using Visual Studio. [Online]. Available at: https://learn.microsoft.com/en-us/azure/azure-functions/create-first-function-vs[Accessed: 6 October 2025].