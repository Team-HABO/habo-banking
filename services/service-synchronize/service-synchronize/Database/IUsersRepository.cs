using service_synchronize.Models;

namespace service_synchronize.Database
{
    public interface IUsersRepository
    {
        Task CreateAccountAsync2(Account newAccount);
        Task CreateAccountAsync(string userId, Account newAccount);
    }
}