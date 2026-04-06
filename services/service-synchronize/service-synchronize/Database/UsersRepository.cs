using MongoDB.Driver;
using service_synchronize.Models;

namespace service_synchronize.Database
{
    public class UsersRepository : IUsersRepository
    {
        private readonly IMongoCollection<User> _usersCollection;
        public UsersRepository(IMongoClient mongoClient, string dbName = "HABO_DB")
        {
            IMongoDatabase database = mongoClient.GetDatabase(dbName);
            _usersCollection = database.GetCollection<User>("users");
        }

        public async Task<User?> GetUserByIdAsync(string userId)
        => await _usersCollection.Find(u => u.Id == userId).FirstOrDefaultAsync();

        public async Task CreateUserWithAccountAsync(User user)
            => await _usersCollection.InsertOneAsync(user);

        public async Task AddAccountToUserAsync(string userId, Account account)
        {
            FilterDefinition<User> filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            UpdateDefinition<User> update = Builders<User>.Update.Push(u => u.Accounts, account);
            await _usersCollection.UpdateOneAsync(filter, update);
        }

    }
}
