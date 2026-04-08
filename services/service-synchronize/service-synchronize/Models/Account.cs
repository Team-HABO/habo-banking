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
        public required string AccountGuid { get; set; }
        public required AccountType Type { get; set; }
        public required string Name { get; set; }
        public required string Timestamp { get; set; }
        public required bool IsFrozen { get; set; }
        public List<Balance> Balances { get; set; } = [];
        public List<Audit> Audits { get; set; } = [];
    }
}
