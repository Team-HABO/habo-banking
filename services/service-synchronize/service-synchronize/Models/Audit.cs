namespace service_synchronize.Models
{
    public class Audit
    {
        public enum AuditType
        {
            Withdraw, Deposit, Transfer
        }
        public required string Amount { get; set; }
        public required AuditType Type { get; set; }
        public required string Timestamp { get; set; }
        public string? ReceiverAccountGuid { get; set; }
    }
}
