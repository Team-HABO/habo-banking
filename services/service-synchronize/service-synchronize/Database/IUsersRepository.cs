using service_synchronize.Models;

namespace service_synchronize.Database
{
    public interface IUsersRepository
    {
        Task<User?> GetUserByIdAsync(string userId);

        Task CreateUserWithAccountAsync(User user);

        Task AddAccountToUserAsync(string userId, Account account);
    }
}