using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using service_synchronize.Database;
using service_synchronize.Models;

namespace service_synchronize.tests.IntegrationDatabase
{
    public abstract class MongoDbIntegrationTestBase : IAsyncLifetime
    {
        private const string DatabaseName = "TestDb";

        protected MongoDbIntegrationTestBase(MongoDbFixture fixture)
        {
            Fixture = fixture;
            Repository = new UsersRepository(Fixture.Client, NullLogger<UsersRepository>.Instance, DatabaseName);
            UserCollection = Fixture.Client.GetDatabase(DatabaseName).GetCollection<User>("users");
        }

        protected MongoDbFixture Fixture { get; }
        protected UsersRepository Repository { get; }
        protected IMongoCollection<User> UserCollection { get; }

        public virtual async Task InitializeAsync()
        {
            await UserCollection.DeleteManyAsync(FilterDefinition<User>.Empty);
        }

        public Task DisposeAsync() => Task.CompletedTask;
    }
}