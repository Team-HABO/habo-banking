namespace service_synchronize.Models
{
    public class Account
    {
        public required string AccountGuid { get; set; }
        public required string Type { get; set; }
        public required string Name { get; set; }
        public required string Timestamp { get; set; }
        public required bool IsFrozen { get; set; }
        public List<Balance> Balances { get; set; } = [];
    }
}
