using service_synchronize.Database;
using service_synchronize.Messages;
using service_synchronize.Models;

namespace service_synchronize.Services
{
    public class AccountService(IUsersRepository repository) : IAccountService
    {
        public async Task ProcessAccountCreationAsync(string ownerId, AccountCreatedAccountDto dto)
        {
            if (dto == null) throw new InvalidDataException("Account data transfer object cannot be null.");
            if (string.IsNullOrWhiteSpace(ownerId)) throw new InvalidDataException($"OwnerId is missing for account {dto.AccountGuid}");

            Account modelAccount = MapAccountToModel(dto);
            await repository.UpsertAccountAsync(ownerId, modelAccount);
        }
        public static Account MapAccountToModel(AccountCreatedAccountDto dto)
        {
            return dto.Balance == null
                ? throw new InvalidDataException($"Balance data is missing for account {dto.AccountGuid}")
                : new Account
                {
                    AccountGuid = dto.AccountGuid,
                    Name = dto.Name,
                    IsFrozen = dto.IsFrozen,
                    Timestamp = dto.Timestamp,

                    Type = Enum.TryParse(dto.Type, true, out Account.AccountType type) ? type : Account.AccountType.Main,
                    Balance = new()
                    {
                        Amount = 0M
                    }

                };
        }
    }
}
