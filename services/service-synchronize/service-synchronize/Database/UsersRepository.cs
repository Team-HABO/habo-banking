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
        public async Task UpsertAccountAsync(string userId, Account account)
        {
            FilterDefinition<User> filter = Builders<User>.Filter.Eq(u => u.Id, userId);

            UpdateDefinition<User> update = Builders<User>.Update
                // If the user is not in database create new document
                .SetOnInsert(u => u.Id, userId)
                // Add the account to the array if AccountGuid is not already there
                .AddToSet(u => u.Accounts, account);

            await _usersCollection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
        }

        public async Task<User?> GetUserByIdAsync(string userId)
        => await _usersCollection.Find(u => u.Id == userId).FirstOrDefaultAsync();
    }
}
