using service_synchronize.Models;

namespace service_synchronize.tests.IntegrationDatabase
{
   public class CreateAccountTests : MongoDbIntegrationTestBase, IClassFixture<MongoDbFixture>
   {
      public CreateAccountTests(MongoDbFixture fixture)
         : base(fixture)
      {
      }

      [Fact]
      public async Task UpsertAccountAsync_ShouldCreateUserAndInsertAccount_WhenUserDoesNotExist()
      {
         Account account = TestData.CreateAccount("account-1");

         await Repository.UpsertAccountAsync("user-1", account);

         User? user = await Repository.GetUserByIdAsync("user-1");

         Assert.NotNull(user);
         Assert.Equal("user-1", user.Id);
         Assert.Single(user.Accounts);
         Assert.Equal("account-1", user.Accounts[0].AccountGuid);
         Assert.Equal("Default Account", user.Accounts[0].Name);
      }

      [Fact]
      public async Task UpsertAccountAsync_ShouldAddAccount_WhenUserAlreadyExists()
      {
         User existingUser = new()
         {
            Id = "user-2",
            Accounts =
            [
               TestData.CreateAccount("account-1", "Primary Account")
            ]
         };

         await UserCollection.InsertOneAsync(existingUser);

         Account secondAccount = TestData.CreateAccount("account-2", "Savings Account");

         await Repository.UpsertAccountAsync("user-2", secondAccount);

         User? user = await Repository.GetUserByIdAsync("user-2");

         Assert.NotNull(user);
         Assert.Equal("user-2", user.Id);
         Assert.Equal(2, user.Accounts.Count);
         Assert.Contains(user.Accounts, account => account.AccountGuid == "account-1");
         Assert.Contains(user.Accounts, account => account.AccountGuid == "account-2");
      }
        [Fact]
        public async Task UpsertAccountAsync_ShouldNotCreateDuplicate_WhenCalledTwiceWithSameAccount()
        {
            Account account = TestData.CreateAccount("account-1");

            await Repository.UpsertAccountAsync("user-1", account);
            await Repository.UpsertAccountAsync("user-1", account);

            User? user = await Repository.GetUserByIdAsync("user-1");

            Assert.Single(user!.Accounts);
        }
    }
}
