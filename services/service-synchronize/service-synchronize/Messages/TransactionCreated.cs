using System.Text.Json.Serialization;

namespace service_synchronize.Messages
{
    public class TransactionCreated
    {
        public required TransactionCreatedData Data { get; set; }
        public required TransactionMetadata Metadata { get; set; }
    }
    public class TransactionCreatedData
    {
        public required string OwnerId { get; set; }
        public required TransactionCreatedAccountDto Account { get; set; }
        public TransactionCreatedAccountDto? Receiver { get; set; }
    }
    public class TransactionCreatedAccountDto
    {
        public required string Guid { get; set; }
        public required AuditDto Audit { get; set; }

    }
    public class AuditDto
    {
        public required string Amount { get; set; }
        public required string Type { get; set; }
        public required string Timestamp { get; set; }
        public string? ReceiverAccountGuid { get; set; }
        [JsonPropertyName("receiver")]
        public string? ReceiverAccountName { get; set; }

    }
    public class TransactionMetadata
    {
        public required string MessageType { get; set; }
        public required string MessageTimestamp { get; set; }
        public required string MessageId { get; set; }
    }
    

}
