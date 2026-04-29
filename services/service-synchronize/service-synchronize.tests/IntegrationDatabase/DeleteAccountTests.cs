using service_synchronize.Models;

namespace service_synchronize.tests.IntegrationDatabase
{
    public class DeleteAccountTests : MongoDbIntegrationTestBase, IClassFixture<MongoDbFixture>
    {
        public DeleteAccountTests(MongoDbFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public async Task DeleteAccountAsync_ShouldRemoveAccountFromExistingUser()
        {
            User existingUser = new()
            {
                Id = "user-1",
                Accounts =
                [
                    TestData.CreateAccount("account-1", "Primary Account"),
                    TestData.CreateAccount("account-2", "Savings Account")
                ]
            };

            await UserCollection.InsertOneAsync(existingUser);

            await Repository.DeleteAccountAsync("user-1", "account-1");

            User? user = await Repository.GetUserByIdAsync("user-1");

            Assert.NotNull(user);
            Assert.Equal("user-1", user.Id);
            Assert.Single(user.Accounts);
            Assert.DoesNotContain(user.Accounts, account => account.AccountGuid == "account-1");
            Assert.Contains(user.Accounts, account => account.AccountGuid == "account-2");
        }

        [Fact]
        public async Task DeleteAccountAsync_ShouldDoNothing_WhenUserDoesNotExist()
        {
            await Repository.DeleteAccountAsync("missing-user", "account-1");

            User? user = await Repository.GetUserByIdAsync("missing-user");

            Assert.Null(user);
        }
    }
}
