using service_synchronize.Models;

namespace service_synchronize.Database
{
    public interface IAccountsRepository
    {
        Task CreateAccountAsync(Account newAccount);
    }
}