
using service_synchronize.Models;

namespace service_synchronize.tests.IntegrationDatabase
{
    public class UpdateAccountStatus : MongoDbIntegrationTestBase, IClassFixture<MongoDbFixture>
    {
        public UpdateAccountStatus(MongoDbFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public async Task UpdateAccountStatusAsync_ShouldUpdateFrozenState_WhenIncomingTimestampIsNewer()
        {
            string existingTimestamp = DateTime.UtcNow.AddMinutes(-10).ToString("O");

            await UserCollection.InsertOneAsync(new User
            {
                Id = "user-1",
                Accounts =
                [
                    new Account
                    {
                        AccountGuid = "account-1",
                        Name = "Primary Account",
                        Type = Account.AccountType.Savings,
                        Timestamp = existingTimestamp,
                        IsFrozen = false,
                        Balance = new Balance { Amount = 0M },
                        Audits = []
                    }
                ]
            });

            string incomingTimestamp = DateTime.UtcNow.ToString("O");

            await Repository.UpdateAccountStatusAsync("user-1", "account-1", true, incomingTimestamp);

            User? user = await Repository.GetUserByIdAsync("user-1");

            Assert.NotNull(user);
            Assert.Single(user!.Accounts);

            Account account = user.Accounts[0];
            Assert.True(account.IsFrozen);
            Assert.Equal(existingTimestamp, account.Timestamp);
            Assert.Equal("Primary Account", account.Name);
        }

        [Fact]
        public async Task UpdateAccountStatusAsync_ShouldNotUpdateFrozenState_WhenIncomingTimestampIsOlder()
        {
            string existingTimestamp = DateTime.UtcNow.ToString("O");

            await UserCollection.InsertOneAsync(new User
            {
                Id = "user-1",
                Accounts =
                [
                    new Account
                    {
                        AccountGuid = "account-1",
                        Name = "Primary Account",
                        Type = Account.AccountType.Savings,
                        Timestamp = existingTimestamp,
                        IsFrozen = false,
                        Balance = new Balance { Amount = 0M },
                        Audits = []
                    }
                ]
            });

            string staleTimestamp = DateTime.UtcNow.AddMinutes(-10).ToString("O");

            await Repository.UpdateAccountStatusAsync("user-1", "account-1", true, staleTimestamp);

            User? user = await Repository.GetUserByIdAsync("user-1");

            Assert.NotNull(user);
            Assert.Single(user!.Accounts);

            Account account = user.Accounts[0];
            Assert.False(account.IsFrozen);
            Assert.Equal(existingTimestamp, account.Timestamp);
            Assert.Equal("Primary Account", account.Name);
        }
    }
}
