using service_synchronize.Database;
using service_synchronize.Messages;
using service_synchronize.Models;
using Microsoft.Extensions.Logging;
using System.Globalization;

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

            string normalizedTimestamp = NormalizeTimestampOrThrow(
                accountDto.Timestamp,
                accountDto.AccountGuid,
                "Rejected account creation for account {AccountGuid}: invalid Timestamp '{Timestamp}'.",
                "Invalid or missing ISO 8601 Timestamp.");

            Account account = new()
            {
                AccountGuid = accountDto.AccountGuid,
                Name = accountDto.Name ?? "Unknown Account",
                IsFrozen = accountDto.IsFrozen ?? false,
                Timestamp = normalizedTimestamp,  // was: accountDto.Timestamp
                Type = Enum.TryParse(accountDto.Type, true, out Account.AccountType type) ? type : Account.AccountType.Checking,
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
                throw new InvalidDataException("Account data transfer object cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                logger.LogWarning("Rejected account update for account {AccountGuid} because OwnerId was missing.", newAccount.AccountGuid);
                throw new InvalidDataException($"OwnerId is missing for account {newAccount.AccountGuid}");
            }

            if (string.IsNullOrWhiteSpace(newAccount.AccountGuid))
            {
                logger.LogWarning("Rejected account update because AccountGuid was missing.");
                throw new InvalidDataException("AccountGuid is missing.");
            }

            if(string.IsNullOrWhiteSpace(newAccount.Type))
            {
                logger.LogWarning("Rejected account update for account {AccountGuid}: account type is null or whitespace '{Type}'.", newAccount.AccountGuid, newAccount.Type);
                throw new InvalidDataException($"Account type is null or whitespace '{newAccount.Type}'. Allowed types are: Savings, Pension, Main.");
            }

            if (!Enum.TryParse(newAccount.Type, true, out Account.AccountType accountType))
            {
                logger.LogWarning("Rejected account update for account {AccountGuid}: invalid account type '{Type}'.", newAccount.AccountGuid, newAccount.Type);
                throw new InvalidDataException($"Invalid account type '{newAccount.Type}'. Allowed types are: Savings, Pension, Main.");
            }

            logger.LogInformation("Processing account update for user {UserId} and account {AccountGuid}.", userId, newAccount.AccountGuid);

            string normalizedTimestamp = NormalizeTimestampOrThrow(
                newAccount.Timestamp,
                newAccount.AccountGuid,
                "Rejected account update for account {AccountGuid}: invalid Timestamp '{Timestamp}'.",
                "Invalid or missing ISO 8601 Timestamp.");

            Account account = new()
            {
                AccountGuid = newAccount.AccountGuid,
                Name = newAccount.Name ?? "Unknown Account",
                Type = accountType,
                Timestamp = normalizedTimestamp,
                IsFrozen = newAccount.IsFrozen ?? false,
                Balance = new Balance { Amount = 0M },
                Audits = []
            };

            await repository.UpdateAccountAsync(userId, account);
            logger.LogInformation("Completed account update for user {UserId} and account {AccountGuid}.", userId, newAccount.AccountGuid);
        }

        public async Task ProcessStatusChangeAsync(string userId, string accountGuid, bool isFrozen, string incomingTimestamp)
        {
            if (string.IsNullOrWhiteSpace(incomingTimestamp))
            {
                logger.LogWarning("Rejected status update because MessageTimestamp was missing for account {AccountGuid}.", accountGuid);
                throw new InvalidDataException($"MessageTimestamp is missing for account {accountGuid}");
            }

            string normalizedIncomingTimestamp = NormalizeTimestampOrThrow(
                incomingTimestamp,
                accountGuid,
                "Rejected status update for account {AccountGuid}: invalid MessageTimestamp '{IncomingTimestamp}'.",
                "Invalid or missing ISO 8601 MessageTimestamp.");

            if (string.IsNullOrWhiteSpace(userId))
            {
                logger.LogWarning("Rejected status update for account {AccountGuid} because OwnerId was missing.", accountGuid);
                throw new InvalidDataException($"OwnerId is missing for account {accountGuid}");
            }

            if (string.IsNullOrWhiteSpace(accountGuid))
            {
                logger.LogWarning("Rejected status update because AccountGuid was missing.");
                throw new InvalidDataException("AccountGuid is missing.");
            }

            logger.LogInformation("Processing status change for user {UserId} and account {AccountGuid}. Incoming timestamp: {IncomingTimestamp}, new isFrozen value: {IsFrozen}.", userId, accountGuid, normalizedIncomingTimestamp, isFrozen);

            await repository.UpdateAccountStatusAsync(userId, accountGuid, isFrozen, normalizedIncomingTimestamp);

            logger.LogInformation("Completed status change for user {UserId} and account {AccountGuid}.", userId, accountGuid);
        }

        private string NormalizeTimestampOrThrow(string timestamp, string accountGuid, string warningTemplate, string exceptionMessage)
        {
            string[] acceptedFormats =
            [
                "O",
                "yyyy-MM-dd'T'HH:mm:ssK",
                "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK"
            ];

            bool parsed = DateTimeOffset.TryParseExact(
                timestamp,
                acceptedFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out DateTimeOffset parsedTimestamp);

            if (!parsed)
            {
                logger.LogWarning(warningTemplate, accountGuid, timestamp);
                throw new InvalidDataException(exceptionMessage);
            }

            return parsedTimestamp.UtcDateTime.ToString("o");
        }

    }
}