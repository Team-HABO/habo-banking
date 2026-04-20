using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace service_synchronize.tests.IntegrationDatabase
{
    public class MongoDbFixture : IAsyncLifetime
    {
        public MongoDbContainer Container { get; } = new MongoDbBuilder("mongo:8.0").WithReplicaSet().Build();
        public IMongoClient Client { get; private set; } = default!;

        public async Task InitializeAsync()
        {
            await Container.StartAsync();
            Client = new MongoClient(Container.GetConnectionString());
        }

        public async Task DisposeAsync() => await Container.StopAsync();
    }
}
