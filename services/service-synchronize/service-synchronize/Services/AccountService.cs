using service_synchronize.Database;
using service_synchronize.Messages;
using service_synchronize.Models;
using Microsoft.Extensions.Logging;

namespace service_synchronize.Services
{
    public class AccountService(IUsersRepository repository, ILogger<AccountService> logger) : IAccountService
    {
        public async Task ProcessAccountCreationAsync(string userId, AccountDetail accountDto)
        {
            if (accountDto == null)
            {
                logger.LogWarning("Rejected account creation because the payload was null.");
                throw new InvalidDataException("Account data transfer object cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                logger.LogWarning("Rejected account creation for account {AccountGuid} because OwnerId was missing.", accountDto.AccountGuid);
                throw new InvalidDataException($"OwnerId is missing for account {accountDto.AccountGuid}");
            }

            logger.LogInformation("Processing account creation for user {UserId} and account {AccountGuid}.", userId, accountDto.AccountGuid);

            Account account = new()
            {
                AccountGuid = accountDto.AccountGuid,
                Name = accountDto.Name ?? "Unknown Account",
                IsFrozen = accountDto.IsFrozen ?? false,
                Timestamp = accountDto.Timestamp,
                Type = Enum.TryParse(accountDto.Type, true, out Account.AccountType type) ? type : Account.AccountType.Main,
                Balance = new Balance { Amount = 0M },
                Audits = []
            };

            await repository.UpsertAccountAsync(userId, account);
            logger.LogInformation("Completed account creation for user {UserId} and account {AccountGuid}.", userId, accountDto.AccountGuid);
        }

        public async Task ProcessAccountDeletionAsync(string userId, string accountGuid)
        {
            logger.LogInformation("Processing account deletion for user {UserId} and account {AccountGuid}.", userId, accountGuid);
            await repository.DeleteAccountAsync(userId, accountGuid);
            logger.LogInformation("Completed account deletion for user {UserId} and account {AccountGuid}.", userId, accountGuid);
        }

        public async Task ProcessAccountUpdateAsync(string userId, AccountDetail newAccount)
        {
            if (newAccount == null)
            {
                logger.LogWarning("Rejected account update because the payload was null.");
                throw new InvalidDataException("Account update payload cannot be null.");
            }

            logger.LogInformation("Processing account update for user {UserId} and account {AccountGuid}.", userId, newAccount.AccountGuid);

            Account updateModel = new()
            {
                AccountGuid = newAccount.AccountGuid,
                Name = newAccount.Name ?? "Default Name",
                Timestamp = newAccount.Timestamp ?? DateTime.UtcNow.ToString("O"),
                Type = Enum.TryParse(newAccount.Type, true, out Account.AccountType type) ? type : Account.AccountType.Main,

                // These will not be used by the UpdateAccountAsync in repository
                IsFrozen = false,
                Balance = new Balance { Amount = 0M }
            };

            await repository.UpdateAccountAsync(userId, updateModel);
            logger.LogInformation("Completed account update for user {UserId} and account {AccountGuid}.", userId, newAccount.AccountGuid);
        }

        public async Task ProcessStatusChangeAsync(string userId, string accountGuid, bool isFrozen)
        {
            logger.LogInformation("Processing account status change for user {UserId} and account {AccountGuid}. Frozen: {IsFrozen}.", userId, accountGuid, isFrozen);
            await repository.UpdateAccountStatusAsync(userId, accountGuid, isFrozen);
            logger.LogInformation("Completed account status change for user {UserId} and account {AccountGuid}. Frozen: {IsFrozen}.", userId, accountGuid, isFrozen);
        }

    }
}
