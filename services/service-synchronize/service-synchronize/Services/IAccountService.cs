using service_synchronize.Messages;

namespace service_synchronize.Services
{
    public interface IAccountService
    {
        Task ProcessAccountCreationAsync(string userId, AccountDetail newAccount);
        Task ProcessAccountUpdateAsync(string userId, AccountDetail newAccount);
        Task ProcessStatusChangeAsync(string userId, string accountGuid, bool newStatus);
        Task ProcessAccountDeletionAsync(string userId, string accountGuid);
    }
}