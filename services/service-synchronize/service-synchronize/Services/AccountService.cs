using service_synchronize.Database;
using service_synchronize.Messages;
using service_synchronize.Models;

namespace service_synchronize.Services
{
    public class AccountService(IUsersRepository repository) : IAccountService
    {
        public async Task ProcessAccountCreationAsync(string userId, AccountDto dto)
        {
            if (string.IsNullOrEmpty(userId)) throw new InvalidDataException($"OwnerId is missing for account {dto.AccountGuid}");

            Account modelAccount = MapAccountToModel(dto);
            await repository.UpsertAccountAsync(userId, modelAccount);
        }
        public static Account MapAccountToModel(AccountDto dto)
        {
            return new Account
            {
                AccountGuid = dto.AccountGuid,
                Name = dto.Name,
                IsFrozen = dto.IsFrozen,
                Timestamp = dto.Timestamp,

                Type = Enum.TryParse(dto.Type, true, out Account.AccountType type) ? type : Account.AccountType.Main,

                Balances =
                [
                    new()
                    {
                        Amount = dto.Balance.Amount,
                        Timestamp = dto.Balance.Timestamp
                    }
                ]
            };
            
        }

    }
}
