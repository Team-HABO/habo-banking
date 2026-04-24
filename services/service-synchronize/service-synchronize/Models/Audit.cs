using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace service_synchronize.Models
{
    public class Audit
    {
        public enum AuditType
        {
            Withdraw, Deposit, Transfer, Exchange
        }
        [BsonElement("auditId")]
        public required string AuditId { get; set; }
        [BsonElement("amount")]
        public required string Amount { get; set; }
        [BsonElement("type")]
        [BsonRepresentation(BsonType.String)]
        public required AuditType Type { get; set; }
        [BsonElement("timestamp")]
        public required string Timestamp { get; set; }
        [BsonElement("sender")]
        public string? SenderAccountName { get; set; }
        [BsonElement("receiver")]
        public string? ReceiverAccountName { get; set; }
    }
}
