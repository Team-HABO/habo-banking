using service_synchronize.Models;

namespace service_synchronize.Database
{
    public interface IUsersRepository
    {
        Task<User?> GetUserByIdAsync(string userId);

        Task UpsertAccountAsync(string userId, Account account);
        Task<bool> AuditExistsAsync(string userId, string auditId);
        Task UpdateUserWithNewTransaction(string userId, string accountGuid, decimal amount, Audit newAudit);
        Task<string?> GetUserIdByAccountGuidAsync(string accountGuid);
        Task ExecuteTransferAsync(
            string senderId, string senderAccountGuid, decimal amount, Audit senderAudit,
            string receiverId, string receiverAccountGuid, Audit receiverAudit);
        Task UpdateAccountAsync(string userId, Account account);
        Task UpdateAccountStatusAsync(string userId, string accountGuid, bool isFrozen, string incomingTimestamp);
        Task DeleteAccountAsync(string userId, string accountGuid);
    }
}