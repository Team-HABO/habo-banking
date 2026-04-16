namespace service_synchronize.Messages
{
    public class AccountCreated
    {
        public required AccountCreatedData Data { get; set; }
        public required AccountCreatedMetadata Metadata { get; set; }
    }

    public class AccountCreatedData
    {
        public required string OwnerId { get; set; }
        public required AccountCreatedAccountDto Account { get; set; }
    }
    public class AccountCreatedMetadata
    {
        public required string MessageType { get; set; }
        public required string MessageTimestamp { get; set; }
    }

    public class AccountCreatedAccountDto
    {
        public required string AccountGuid { get; set; }
        public required string Type { get; set; }
        public required string Name { get; set; }
        public required bool IsFrozen { get; set; }
        public required string Timestamp { get; set; }
        public required BalanceDto Balance { get; set; }
    }

    public class BalanceDto
    {
        public required string Amount { get; set; }
    }
}
