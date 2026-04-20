using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace service_synchronize.Models
{
    public class Account
    {
        public enum AccountType
        {
            Savings,
            Pension,
            Main
        }
        [BsonElement("accountGuid")]
        public required string AccountGuid { get; set; }
        [BsonElement("type")]
        [BsonRepresentation(BsonType.String)]
        public required AccountType Type { get; set; }
        [BsonElement("name")]
        public required string Name { get; set; }
        [BsonElement("timestamp")]
        public required string Timestamp { get; set; }
        [BsonElement("isFrozen")]
        public required bool IsFrozen { get; set; }
        [BsonElement("balance")]
        public required Balance Balance { get; set; }
        [BsonElement("audits")]
        public List<Audit> Audits { get; set; } = [];
    }
}
