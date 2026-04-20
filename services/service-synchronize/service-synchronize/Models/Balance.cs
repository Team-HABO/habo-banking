using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace service_synchronize.Models
{
    public class Balance
    {
        [BsonElement("amount")]
        [BsonRepresentation(BsonType.Decimal128)]
        public required decimal Amount { get; set; }
    }
}