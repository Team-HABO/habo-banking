using MongoDB.Driver;
using service_synchronize.Models;

namespace service_synchronize.Database
{
    public class AccountsRepository : IAccountsRepository
    {
        private readonly string dbName = "HABODB";

        private readonly IMongoCollection<Account> _accountsCollection;
        public AccountsRepository(IMongoClient mongoClient)
        {
            IMongoDatabase database = mongoClient.GetDatabase(dbName);
            _accountsCollection = database.GetCollection<Account>("accounts");
        }
        public async Task CreateAccountAsync(Account newAccount)
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
