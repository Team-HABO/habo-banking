namespace service_synchronize.Messages
{
    public class AccountCreated
    {
        public required Data Data { get; set; }
        public required Metadata Metadata { get; set; }
    }

    public class Data
    {
        public required string OwnerId { get; set; }
        public required AccountDto Account { get; set; }
    }
    public class Metadata
    {
        public required string MessageType { get; set; }
        public required DateTime MessageTimestamp { get; set; }
    }

    public class AccountDto
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
        public required string Timestamp { get; set; }
    }
}
