using MongoDB.Driver;
using service_synchronize.Database;
using service_synchronize.Messages;
using service_synchronize.Models;
using service_synchronize.Services;

namespace service_synchronize.tests.IntegrationDatabase
{
    public class CreateAccountTests : IAsyncLifetime
    {
        private readonly MongoClient _client;
        private UsersRepository _repository = default!;
        private AccountService _service = default!;
        private string _dbName = default!;
        private readonly AccountDto firstAccount = TestData.CreateAccountDto("1");
        private readonly AccountDto secondAccount = TestData.CreateAccountDto("2");
        private readonly string mongoConnectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING")
    ?? "mongodb://localhost:27018";
        public CreateAccountTests()
        {
            _client = new MongoClient(mongoConnectionString);
        }
        
        private readonly string userId = "1";

        [Fact]
        public async Task CreateAccount_ShouldCreateUser_WhenUserDoesNotExist()
        {
            await _service.ProcessAccountCreationAsync(userId, firstAccount);
            User? createdUser = await _repository.GetUserByIdAsync(userId);

            Assert.NotNull(createdUser);
            Assert.Equal(userId, createdUser.Id);
            Assert.Single(createdUser.Accounts);
            Assert.Equal("Default Account", createdUser.Accounts[0].Name);
            Assert.Equal("1", createdUser.Accounts[0].AccountGuid);
        }
        [Fact]
        public async Task CreateAccount_ShouldEmbedAccount_WhenUserExistsAndAccountDoesNotExist()
        {
            await _service.ProcessAccountCreationAsync(userId, firstAccount);
            await _service.ProcessAccountCreationAsync(userId, secondAccount);

            User? user = await _repository.GetUserByIdAsync(userId);

            Assert.NotNull(user);
            Assert.Equal(userId, user.Id);
            Assert.Equal(2, user.Accounts.Count);
            Assert.Equal("1", user.Accounts[0].AccountGuid);
            Assert.Equal("2", user.Accounts[1].AccountGuid);

        }
        [Fact]
        public async Task CreateAccount_ShouldDoNothing_WhenAccountAlreadyExists()
        {
            await _service.ProcessAccountCreationAsync(userId, firstAccount);
            await _service.ProcessAccountCreationAsync(userId, secondAccount);
            await _service.ProcessAccountCreationAsync(userId, secondAccount);

            User? user = await _repository.GetUserByIdAsync(userId);

            Assert.NotNull(user);
            Assert.Equal(userId, user.Id);
            Assert.Equal(2, user.Accounts.Count);
            Assert.Equal("1", user.Accounts[0].AccountGuid);
            Assert.Equal("2", user.Accounts[1].AccountGuid);

        }

        public Task InitializeAsync()
        {
            _dbName = "TestDB_" + Guid.NewGuid().ToString("N");
            _repository = new UsersRepository(_client, _dbName);
            _service = new AccountService(_repository);

            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await _client.DropDatabaseAsync(_dbName);
        }
    }
}
