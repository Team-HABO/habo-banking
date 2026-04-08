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
            //Can not call GetUserByIdAsync before an update because it introduces a Race Condition
            FilterDefinition<User> userFilter = Builders<User>.Filter.Eq(u => u.Id, userId);
            UpdateDefinition<User> userUpdate = Builders<User>.Update.SetOnInsert(u => u.Id, userId);

            await _usersCollection.UpdateOneAsync(userFilter, userUpdate, new UpdateOptions { IsUpsert = true });

            FilterDefinition<User> accountFilter = Builders<User>.Filter.And(
                Builders<User>.Filter.Eq(u => u.Id, userId),
                Builders<User>.Filter.Not(
                    Builders<User>.Filter.ElemMatch(u => u.Accounts, a => a.AccountGuid == account.AccountGuid)
                )
            );

            var accountUpdate = Builders<User>.Update.Push(u => u.Accounts, account);

            await _usersCollection.UpdateOneAsync(accountFilter, accountUpdate, new UpdateOptions { IsUpsert = false });
        }

        public async Task<User?> GetUserByIdAsync(string userId)
        => await _usersCollection.Find(u => u.Id == userId).FirstOrDefaultAsync();
    }
}
