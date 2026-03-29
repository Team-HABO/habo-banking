
namespace service_synchronize.Models
{
    public class AccountCreated
    {
        public string UserId { get; set; }
        public required Account NewAccount { get; set; }
        public required Metadata Metadata { get; set; }
    }
}
