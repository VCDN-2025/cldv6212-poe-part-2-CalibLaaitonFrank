using ABCRetailPOE.Models;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using System.Web;

namespace AzureFunctionPOE
{
	public class TableStorageFunction
	{
		private readonly ILogger _logger;

		public TableStorageFunction(ILoggerFactory loggerFactory)
		{
			_logger = loggerFactory.CreateLogger<TableStorageFunction>();
		}

		// Base URL endpoint
		[Function("HealthCheck")]
		public HttpResponseData HealthCheck(
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "")] HttpRequestData req)
		{
			_logger.LogInformation("Health check endpoint called");

			var response = req.CreateResponse(HttpStatusCode.OK);
			response.Headers.Add("Content-Type", "application/json");

			var welcomeMessage = new
			{
				message = "Table Storage Function API is running!",
				endpoints = new[] {
					"GET    /api/ - Health check (this page)",
					"POST   /api/AddCustomers - Add a new customer",
					"GET    /api/GetCustomers - Get all customers",
					"GET    /api/SearchCustomers?searchTerm=name - Search customers"
				},
				timestamp = DateTime.UtcNow
			};

			response.WriteString(JsonSerializer.Serialize(welcomeMessage, new JsonSerializerOptions { WriteIndented = true }));
			return response;
		}

		[Function("AddCustomers")]
		public async Task<HttpResponseData> AddCustomer(
			[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "AddCustomers")] HttpRequestData req)
		{
			try
			{
				var body = await new StreamReader(req.Body).ReadToEndAsync();

				if (string.IsNullOrEmpty(body))
				{
					var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
					await errorResponse.WriteStringAsync("Request body is empty");
					return errorResponse;
				}

				var customer = JsonSerializer.Deserialize<Customer>(body);

				if (customer == null)
				{
					var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
					await errorResponse.WriteStringAsync("Invalid customer data");
					return errorResponse;
				}

				// Create TableServiceClient 
				var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? "UseDevelopmentStorage=true";
				var tableServiceClient = new TableServiceClient(connectionString);

				var tableClient = tableServiceClient.GetTableClient("customers");
				await tableClient.CreateIfNotExistsAsync();
				await tableClient.AddEntityAsync(customer);

				_logger.LogInformation("Added new customer: {FirstName} {LastName}", customer.FirstName, customer.LastName);

				var response = req.CreateResponse(HttpStatusCode.OK);
				response.Headers.Add("Content-Type", "application/json");
				await response.WriteStringAsync(JsonSerializer.Serialize(new
				{
					message = "Customer added successfully",
					customerId = customer.RowKey
				}));
				return response;
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error adding customer: {ex.Message}");
				var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
				await errorResponse.WriteStringAsync($"Error: {ex.Message}");
				return errorResponse;
			}
		}

		[Function("GetCustomers")]
		public async Task<HttpResponseData> GetCustomers(
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "GetCustomers")] HttpRequestData req)
		{
			try
			{
				// Create TableServiceClient
				var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? "UseDevelopmentStorage=true";
				var tableServiceClient = new TableServiceClient(connectionString);

				var tableClient = tableServiceClient.GetTableClient("customers");
				var customers = new List<Customer>();

				await foreach (var customer in tableClient.QueryAsync<Customer>())
				{
					customers.Add(customer);
				}

				var response = req.CreateResponse(HttpStatusCode.OK);
				response.Headers.Add("Content-Type", "application/json");
				await response.WriteStringAsync(JsonSerializer.Serialize(customers));
				return response;
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error getting customers: {ex.Message}");
				var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
				await errorResponse.WriteStringAsync($"Error: {ex.Message}");
				return errorResponse;
			}
		}

		[Function("SearchCustomers")]
		public async Task<HttpResponseData> SearchCustomers(
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "SearchCustomers")] HttpRequestData req)
		{
			try
			{
				var query = HttpUtility.ParseQueryString(req.Url.Query);
				var searchTerm = query["searchTerm"];

				if (string.IsNullOrEmpty(searchTerm))
				{
					var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
					await errorResponse.WriteStringAsync("searchTerm parameter is required");
					return errorResponse;
				}

				// Create TableServiceClient 
				var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? "UseDevelopmentStorage=true";
				var tableServiceClient = new TableServiceClient(connectionString);

				var tableClient = tableServiceClient.GetTableClient("customers");
				var customers = new List<Customer>();

				await foreach (var customer in tableClient.QueryAsync<Customer>())
				{
					if ((customer.FirstName?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true ||
						 customer.LastName?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true ||
						 customer.Email?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true))
					{
						customers.Add(customer);
					}
				}

				var response = req.CreateResponse(HttpStatusCode.OK);
				response.Headers.Add("Content-Type", "application/json");
				await response.WriteStringAsync(JsonSerializer.Serialize(customers));
				return response;
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error searching customers: {ex.Message}");
				var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
				await errorResponse.WriteStringAsync($"Error: {ex.Message}");
				return errorResponse;
			}
		}
	}
}