
namespace service_synchronize.Messages
{
    public class AccountEventEnvelope
    {
        public required AccountEventData Data { get; set; }
        public required AccountMetadata Metadata { get; set; }
    }

    public class AccountMetadata
    {
        public required string MessageType { get; set; }
        public required string MessageTimestamp { get; set; }
    }
    public class AccountEventData
    {
        public required string OwnerId { get; set; }
        public required AccountDetail Account { get; set; }
    }

    public class AccountDetail
    {
        public required string AccountGuid { get; set; }
        public required string Timestamp { get; set; }

        // Nullable fields for properties that only appear in specific events
        public string? Type { get; set; }
        public string? Name { get; set; }
        public bool? IsFrozen { get; set; }
        public BalanceDto? Balance { get; set; }
    }

    public class BalanceDto
    {
        public required string Amount { get; set; }
    }
}
