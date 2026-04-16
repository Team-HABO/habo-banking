using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace service_synchronize.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public required string Id { get; set; }
        [BsonElement("accounts")]
        public required List<Account> Accounts { get; set; }
    }
}
