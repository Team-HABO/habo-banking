using service_synchronize.Messages;

namespace service_synchronize.Services
{
    public interface IAccountService
    {
        Task ProcessAccountCreationAsync(string userId, AccountCreatedAccountDto newAccount);
    }
}