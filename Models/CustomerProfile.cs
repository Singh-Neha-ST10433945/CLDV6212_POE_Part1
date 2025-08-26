using Azure;
using Azure.Data.Tables;

namespace ABCretailStorageApp.Models
{
    public class CustomerProfile : ITableEntity
    {
        public string PartitionKey { get; set; } = "Customer";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string FavoriteProduct { get; set; } = "";
        public string LoyaltyTier { get; set; } = "Bronze";
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
