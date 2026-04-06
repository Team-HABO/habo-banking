namespace service_synchronize.Models
{
    public class User
    {
        public required string Id { get; set; }
        public required List<Account> Accounts { get; set; }
    }
}
