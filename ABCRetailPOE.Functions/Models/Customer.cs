using Azure;
using Azure.Data.Tables;

namespace ABCRetailPOE.Models
{
	public class Customer : ITableEntity
	{
		//properties
		public string PartitionKey { get; set; } = "CUSTOMER";  
		public string RowKey { get; set; } = Guid.NewGuid().ToString(); 
		public DateTimeOffset? Timestamp { get; set; }        
		public ETag ETag { get; set; }                         

		//info
		public string FirstName { get; set; } = string.Empty;
		public string LastName { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public string PhoneNumber { get; set; } = string.Empty;
		public string Address { get; set; } = string.Empty;
		public object FullName { get; internal set; }
	}
}
