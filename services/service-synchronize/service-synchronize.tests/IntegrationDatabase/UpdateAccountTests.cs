
using service_synchronize.Models;

namespace service_synchronize.tests.IntegrationDatabase
{
    public class UpdateAccountTests : MongoDbIntegrationTestBase, IClassFixture<MongoDbFixture>
    {
        public UpdateAccountTests(MongoDbFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public async Task UpdateAccountAsync_ShouldUpdateAccount_WhenIncomingTimestampIsNewer()
        {
            Account existingAccount = new()
            {
                AccountGuid = "account-1",
                Name = "Old Account Name",
                Type = Account.AccountType.Savings,
                Timestamp = DateTime.UtcNow.AddMinutes(-10).ToString("O"),
                IsFrozen = false,
                Balance = new Balance { Amount = 0M },
                Audits = []
            };

            await UserCollection.InsertOneAsync(new User
            {
                Id = "user-1",
                Accounts = [existingAccount]
            });

            Account updatedAccount = new()
            {
                AccountGuid = "account-1",
                Name = "New Account Name",
                Type = Account.AccountType.Pension,
                Timestamp = DateTime.UtcNow.ToString("O"),
                IsFrozen = true,
                Balance = new Balance { Amount = 0M },
                Audits = []
            };

            await Repository.UpdateAccountAsync("user-1", updatedAccount);

            User? user = await Repository.GetUserByIdAsync("user-1");

            Assert.NotNull(user);
            Assert.Single(user!.Accounts);

            Account account = user.Accounts[0];
            Assert.Equal("account-1", account.AccountGuid);
            Assert.Equal("New Account Name", account.Name);
            Assert.Equal(Account.AccountType.Pension, account.Type);
            Assert.Equal(updatedAccount.Timestamp, account.Timestamp);
            Assert.True(account.IsFrozen);
        }

        [Fact]
        public async Task UpdateAccountAsync_ShouldNotUpdateAccount_WhenIncomingTimestampIsOlder()
        {
            string existingTimestamp = DateTime.UtcNow.ToString("O");

            Account existingAccount = new()
            {
                AccountGuid = "account-1",
                Name = "Current Account Name",
                Type = Account.AccountType.Savings,
                Timestamp = existingTimestamp,
                IsFrozen = false,
                Balance = new Balance { Amount = 0M },
                Audits = []
            };

            await UserCollection.InsertOneAsync(new User
            {
                Id = "user-1",
                Accounts = [existingAccount]
            });

            Account staleAccount = new()
            {
                AccountGuid = "account-1",
                Name = "Stale Account Name",
                Type = Account.AccountType.Pension,
                Timestamp = DateTime.UtcNow.AddMinutes(-10).ToString("O"),
                IsFrozen = true,
                Balance = new Balance { Amount = 0M },
                Audits = []
            };

            await Repository.UpdateAccountAsync("user-1", staleAccount);

            User? user = await Repository.GetUserByIdAsync("user-1");

            Assert.NotNull(user);
            Assert.Single(user!.Accounts);

            Account account = user.Accounts[0];
            Assert.Equal("account-1", account.AccountGuid);
            Assert.Equal("Current Account Name", account.Name);
            Assert.Equal(Account.AccountType.Savings, account.Type);
            Assert.Equal(existingTimestamp, account.Timestamp);
            Assert.False(account.IsFrozen);
        }
    }
}
