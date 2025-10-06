using Azure;
using Azure.Data.Tables;

namespace ABCRetailPOE.Models
{
	public class Product : ITableEntity
	{
		//properties
		public string PartitionKey { get; set; } = "PRODUCT";
		public string RowKey { get; set; } = Guid.NewGuid().ToString();
		public DateTimeOffset? Timestamp { get; set; }
		public ETag ETag { get; set; }

		//info
		public string ProductName { get; set; } = string.Empty;
		public string Description { get; set; } = string.Empty;
		public double Price { get; set; } = 0;
		public int StockQuantity { get; set; } = 0;
		public string ImageUrl { get; set; } = string.Empty;

	}
}
