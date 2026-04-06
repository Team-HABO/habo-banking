using service_synchronize.Database;
using service_synchronize.Messages;
using service_synchronize.Models;

namespace service_synchronize.Services
{
    public class AccountService(IUsersRepository repository) : IAccountService
    {
        public async Task ProcessAccountCreationAsync(string userId, AccountDto dto)
        {
            if (string.IsNullOrEmpty(userId)) throw new ArgumentException("User ID is required.");

            User? user = await repository.GetUserByIdAsync(userId);
            Account modelAccount = MapAccountToModel(dto);

            if (user == null)
            {
                User newUser = new() { Id = userId, Accounts = [modelAccount] };
                await repository.CreateUserWithAccountAsync(newUser);
                return;
            }

            bool alreadyExists = user.Accounts.Any(a => a.AccountGuid == modelAccount.AccountGuid);
            if (!alreadyExists)
            {
                await repository.AddAccountToUserAsync(userId, modelAccount);
            }
        }
        public static Account MapAccountToModel(AccountDto dto)
        {
            return new Account
            {
                AccountGuid = dto.AccountGuid,
                Name = dto.Name,
                IsFrozen = dto.IsFrozen,
                Timestamp = dto.Timestamp,

                Type = Enum.TryParse<Account.AccountType>(dto.Type, true, out var type)
            ? type
            : Account.AccountType.Main,

                    Balances = new List<Balance>
            {
                new Balance
                {
                    Amount = dto.Balance.Amount,
                    Timestamp = dto.Balance.Timestamp
                }
            }
                };
            }

    }
}
