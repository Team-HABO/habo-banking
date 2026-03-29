using System.Security.Principal;
using MongoDB.Driver;
using service_synchronize.Models;

namespace service_synchronize.Database
{
    public class UsersRepository : IUsersRepository
    {
        private readonly string dbName = "HABODB";

        private readonly IMongoCollection<User> _usersCollection;
        private readonly IMongoCollection<Account> _accountsCollection;
        public UsersRepository(IMongoClient mongoClient)
        {
            IMongoDatabase database = mongoClient.GetDatabase(dbName);
            _usersCollection = database.GetCollection<User>("users");
        }

        public async Task CreateAccountAsync(string userId, Account newAccount)
        {
            try
            {

                var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
                //embed account
                //await _usersCollection.UpdateOne(newData);

                var update = Builders<User>.Update.Push(u => u.Accounts, newAccount);

                await _usersCollection.UpdateOneAsync(filter, update);
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task CreateAccountAsync2(Account newAccount)
        {
            try
            {
                await _accountsCollection.InsertOneAsync(newAccount);
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
