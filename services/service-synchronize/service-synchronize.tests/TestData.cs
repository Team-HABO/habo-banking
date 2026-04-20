using service_synchronize.Messages;
using service_synchronize.Models;

namespace service_synchronize.tests
{
    public static class TestData
    {
        public static Account CreateAccount(string? guid = null, string name = "Default Account")
        {
            return new Account
            {
                AccountGuid = guid ?? Guid.NewGuid().ToString(),
                Name = name,
                IsFrozen = false,
                Timestamp = DateTime.UtcNow.ToString("O"),
                Type = Account.AccountType.Savings,
                Balance = new() { Amount = 0M }
            };
        }
        public static AccountCreatedAccountDto CreateAccountDto(string? guid = null, string name = "Default Account")
        {
            return new AccountCreatedAccountDto
            {
                AccountGuid = guid ?? Guid.NewGuid().ToString(),
                Name = name,
                IsFrozen = false,
                Timestamp = DateTime.UtcNow.ToString("O"),
                Type = "Savings",
                Balance = new BalanceDto { Amount="0"}
            };
        }
    }
}

