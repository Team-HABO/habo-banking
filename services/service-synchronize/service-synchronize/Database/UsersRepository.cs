using MongoDB.Bson;
using MongoDB.Driver;
using service_synchronize.Models;

namespace service_synchronize.Database
{
    public class UsersRepository : IUsersRepository
    {
        private readonly IMongoCollection<User> _usersCollection;
        private readonly IMongoClient _client;
        public UsersRepository(IMongoClient mongoClient, string dbName = "HABO_DB")
        {
            IMongoDatabase database = mongoClient.GetDatabase(dbName);
            _usersCollection = database.GetCollection<User>("users");
            _client = mongoClient;
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

            UpdateDefinition<User> accountUpdate = Builders<User>.Update.Push(u => u.Accounts, account);

            await _usersCollection.UpdateOneAsync(accountFilter, accountUpdate, new UpdateOptions { IsUpsert = false });
        }

        public async Task<User?> GetUserByIdAsync(string userId)
        => await _usersCollection.Find(u => u.Id == userId).FirstOrDefaultAsync();

        public async Task<bool> AuditExistsAsync(string userId, string auditId)
        {
            FilterDefinition<User> filter = Builders<User>.Filter.And(
                    Builders<User>.Filter.Eq(u => u.Id, userId),
                    Builders<User>.Filter.ElemMatch(
                        u => u.Accounts,
                        a => a.Audits.Any(audit => audit.AuditId == auditId)
                    )
                );

            return await _usersCollection.Find(filter).AnyAsync();
        }
        public async Task UpdateUserWithNewTransaction(string userId, string accountGuid, decimal amount, Audit newAudit)
        {
            FilterDefinition<User> filter = Builders<User>.Filter.And(
                Builders<User>.Filter.Eq(u => u.Id, userId),
                Builders<User>.Filter.Eq("accounts.accountGuid", accountGuid));

            UpdateDefinition<User> update = Builders<User>.Update
                .Push("accounts.$.audits", newAudit)
                .Inc("accounts.$.balance.amount", amount);

            UpdateResult result = await _usersCollection.UpdateOneAsync(filter, update);

            if (result.MatchedCount == 0)
                throw new InvalidOperationException($"No account found for userId '{userId}' and accountGuid '{accountGuid}'.");
        }
        public async Task<string?> GetUserIdByAccountGuidAsync(string accountGuid)
        {
            FilterDefinition<User> filter = Builders<User>.Filter.Eq("accounts.accountGuid", accountGuid);

            ProjectionDefinition<User> projection = Builders<User>.Projection.Include(u => u.Id);

            BsonDocument result = await _usersCollection.Find(filter)
                                         .Project(projection)
                                         .FirstOrDefaultAsync();

            return result?["_id"]?.ToString();
        }

        public async Task ExecuteTransferAsync(
            string senderId, string senderAccountGuid, decimal amount, Audit senderAudit,
            string receiverId, string receiverAccountGuid, Audit receiverAudit)
        {
            using IClientSessionHandle session = await _client.StartSessionAsync();
            session.StartTransaction();

            try
            {
                await UpdateWithSession(session, senderId, senderAccountGuid, amount * -1, senderAudit);
                await UpdateWithSession(session, receiverId, receiverAccountGuid, amount, receiverAudit);

                await session.CommitTransactionAsync();
            }
            catch (Exception)
            {
                await session.AbortTransactionAsync();
                throw; 
            }
        }

        private async Task UpdateWithSession(IClientSessionHandle session, string userId, string accountGuid, decimal amount, Audit audit)
        {
            FilterDefinition<User> filter = Builders<User>.Filter.And(
                Builders<User>.Filter.Eq(u => u.Id, userId),
                Builders<User>.Filter.Eq("accounts.accountGuid", accountGuid));
            UpdateDefinition<User> update = Builders<User>.Update
                .Push("accounts.$.audits", audit)
                .Inc("accounts.$.balance.amount", amount);

            UpdateResult result = await _usersCollection.UpdateOneAsync(session, filter, update);
            if (result.MatchedCount == 0)
            {
                throw new InvalidOperationException(
                    $"Account not found for userId '{userId}' and accountGuid '{accountGuid}' during session update.");
            }
        }
    }
}
