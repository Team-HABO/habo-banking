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
            FilterDefinition<User> filter = Builders<User>.Filter.And(
                Builders<User>.Filter.Eq(u => u.Id, userId),
                Builders<User>.Filter.Not(
                    Builders<User>.Filter.ElemMatch(u => u.Accounts, a => a.AccountGuid == account.AccountGuid)
                )
            );

            UpdateDefinition<User> update = Builders<User>.Update.SetOnInsert(u => u.Id, userId).Push(u => u.Accounts, account);

            try
            {
                await _usersCollection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                return;
            }
        }

        public async Task<User?> GetUserByIdAsync(string userId)
        => await _usersCollection.Find(u => u.Id == userId).FirstOrDefaultAsync();
    }
}
