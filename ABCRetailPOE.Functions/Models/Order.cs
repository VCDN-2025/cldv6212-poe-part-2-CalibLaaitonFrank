using Azure;
using Azure.Data.Tables;

namespace ABCRetailPOE.Models
{
	public class Order : ITableEntity
	{
		//properties
		public string PartitionKey { get; set; } = "ORDER";
		public string RowKey { get; set; } = Guid.NewGuid().ToString();
		public DateTimeOffset? Timestamp { get; set; }
		public ETag ETag { get; set; }

		//info
		public string FirstName { get; set; } = string.Empty; //Customer
		public string ProductName { get; set; } = string.Empty; //Product
		public int Quantity { get; set; } = 0;
		public DateTime OrderDate { get; set; } = DateTime.UtcNow;
		public string Status { get; set; } = "PENDING";
		public string ShippingAddress { get; set; } = string.Empty;

		public decimal TotalPrice { get; set; }
	}
}
