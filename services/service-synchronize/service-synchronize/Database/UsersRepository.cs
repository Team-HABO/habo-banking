using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using service_synchronize.Models;

namespace service_synchronize.Database
{
    public class UsersRepository : IUsersRepository
    {
        private readonly IMongoCollection<User> _usersCollection;
        private readonly IMongoClient _client;
        private readonly ILogger<UsersRepository> _logger;
        public UsersRepository(IMongoClient mongoClient, ILogger<UsersRepository> logger, string dbName = "HABO_DB")
        {
            IMongoDatabase database = mongoClient.GetDatabase(dbName);
            _usersCollection = database.GetCollection<User>("users");
            _client = mongoClient;
            _logger = logger;
        }
        public async Task DeleteAccountAsync(string userId, string accountGuid)
        {
            _logger.LogInformation("Deleting account {AccountGuid} for user {UserId}.", accountGuid, userId);

            FilterDefinition<User> filter = Builders<User>.Filter.Eq(u => u.Id, userId);

            UpdateDefinition<User> update = Builders<User>.Update.PullFilter(
                u => u.Accounts,
                a => a.AccountGuid == accountGuid
            );

            UpdateResult result = await _usersCollection.UpdateOneAsync(filter, update);

            if (result.ModifiedCount == 0)
            {
                _logger.LogWarning("Delete failed: Account {AccountGuid} was not found for user {UserId}.", accountGuid, userId);
                return;
            }

            _logger.LogInformation("Deleted account {AccountGuid} for user {UserId}.", accountGuid, userId);
        }

        public async Task UpsertAccountAsync(string userId, Account account)
        {
            //Can not call GetUserByIdAsync before an update because it introduces a Race Condition
            _logger.LogInformation("Inserting account {AccountGuid} for user {UserId}.", account.AccountGuid, userId);

            FilterDefinition<User> userFilter = Builders<User>.Filter.Eq(u => u.Id, userId);
            UpdateDefinition<User> userUpdate = Builders<User>.Update.SetOnInsert(u => u.Id, userId);

            UpdateResult userResult = await _usersCollection.UpdateOneAsync(userFilter, userUpdate, new UpdateOptions { IsUpsert = true });

            if (userResult.UpsertedId != null)
            {
                _logger.LogInformation("Created user {UserId} while inserting account {AccountGuid}.", userId, account.AccountGuid);
            }

            FilterDefinition<User> accountFilter = Builders<User>.Filter.And(
                Builders<User>.Filter.Eq(u => u.Id, userId),
                Builders<User>.Filter.Not(
                    Builders<User>.Filter.ElemMatch(u => u.Accounts, a => a.AccountGuid == account.AccountGuid)
                )
            );

            UpdateDefinition<User> accountUpdate = Builders<User>.Update.Push(u => u.Accounts, account);

            UpdateResult accountResult = await _usersCollection.UpdateOneAsync(accountFilter, accountUpdate, new UpdateOptions { IsUpsert = false });

            if (accountResult.MatchedCount == 0)
            {
                _logger.LogDebug("Account {AccountGuid} already exists for user {UserId}; upsert completed without changes.", account.AccountGuid, userId);
                return;
            }

            _logger.LogInformation("Inserted account {AccountGuid} for user {UserId}.", account.AccountGuid, userId);
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
                Builders<User>.Filter.Eq("accounts.accountGuid", accountGuid),
                Builders<User>.Filter.Not(
                    Builders<User>.Filter.ElemMatch(
                        "accounts.audits",
                        Builders<BsonDocument>.Filter.Eq("auditId", newAudit.AuditId)
                    )
                )
            );

            UpdateDefinition <User> update = Builders<User>.Update
                .Push("accounts.$.audits", newAudit)
                .Inc("accounts.$.balance.amount", amount);

            UpdateResult result = await _usersCollection.UpdateOneAsync(filter, update);

            if (result.MatchedCount == 0)
            {
                if (await AuditExistsAsync(userId, newAudit.AuditId))
                {
                    return;
                }
                _logger.LogWarning("Transaction update failed: account {AccountGuid} not found for user {UserId}.", accountGuid, userId);
                throw new InvalidOperationException($"No account found for userId '{userId}' and accountGuid '{accountGuid}'.");
            }

            _logger.LogInformation("Applied transaction audit {AuditId} to account {AccountGuid} for user {UserId}.", newAudit.AuditId, accountGuid, userId);
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
            _logger.LogInformation(
                "Starting transfer from user {SenderId} account {SenderAccountGuid} to user {ReceiverId} account {ReceiverAccountGuid} for amount {Amount}.",
                senderId, senderAccountGuid, receiverId, receiverAccountGuid, amount);

            using IClientSessionHandle session = await _client.StartSessionAsync();
            session.StartTransaction();

            try
            {
                await UpdateWithSession(session, senderId, senderAccountGuid, amount * -1, senderAudit);
                await UpdateWithSession(session, receiverId, receiverAccountGuid, amount, receiverAudit);

                await session.CommitTransactionAsync();
                _logger.LogInformation(
                    "Completed transfer from user {SenderId} account {SenderAccountGuid} to user {ReceiverId} account {ReceiverAccountGuid} for amount {Amount}.",
                    senderId, senderAccountGuid, receiverId, receiverAccountGuid, amount);
            }
            catch (Exception ex)
            {
                await session.AbortTransactionAsync();
                _logger.LogError(
                    ex,
                    "Transfer failed between user {SenderId} account {SenderAccountGuid} and user {ReceiverId} account {ReceiverAccountGuid} for amount {Amount}.",
                    senderId, senderAccountGuid, receiverId, receiverAccountGuid, amount);
                throw;
            }
        }

        private async Task UpdateWithSession(IClientSessionHandle session, string userId, string accountGuid, decimal amount, Audit audit)
        {
            FilterDefinition<User> filter = Builders<User>.Filter.And(
               Builders<User>.Filter.Eq(u => u.Id, userId),
               Builders<User>.Filter.Eq("accounts.accountGuid", accountGuid),
               Builders<User>.Filter.Not(
                   Builders<User>.Filter.ElemMatch(
                       "accounts.audits",
                       Builders<BsonDocument>.Filter.Eq("auditId", audit.AuditId)
                   )
               )
           );

            UpdateDefinition<User> update = Builders<User>.Update
                .Push("accounts.$.audits", audit)
                .Inc("accounts.$.balance.amount", amount);

            UpdateResult result = await _usersCollection.UpdateOneAsync(session, filter, update);
            if (result.MatchedCount == 0)
            {
                if (await AuditExistsAsync(userId, audit.AuditId))
                {
                    return;
                }
                _logger.LogWarning(
                    "Session update failed: account {AccountGuid} not found for user {UserId}.",
                    accountGuid, userId);
                throw new InvalidOperationException(
                    $"Account not found for userId '{userId}' and accountGuid '{accountGuid}' during session update.");
            }

            _logger.LogInformation("Applied session update for user {UserId} account {AccountGuid}.", userId, accountGuid);
        }

        public async Task UpdateAccountAsync(string userId, Account account)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("Update account failed: UserId is null or empty.");
                throw new InvalidOperationException("UserId cannot be null or empty.");
            }

            if (account == null)
            {
                _logger.LogWarning("Update account failed: Account object is null.");
                throw new InvalidOperationException("Account object cannot be null.");
            }

            _logger.LogInformation("Updating account {AccountGuid} for user {UserId}.", account.AccountGuid, userId);

           

            FilterDefinition<User> filter = Builders<User>.Filter.And(
                Builders<User>.Filter.Eq(u => u.Id, userId),
                Builders<User>.Filter.ElemMatch(u => u.Accounts, a => 
                    a.AccountGuid == account.AccountGuid && 
                    a.Timestamp.CompareTo(account.Timestamp) < 0) 
            );

            UpdateDefinition<User> update = Builders<User>.Update
                .Set("accounts.$.name", account.Name)
                .Set("accounts.$.type", account.Type.ToString())
                .Set("accounts.$.timestamp", account.Timestamp)
                .Set("accounts.$.isFrozen", account.IsFrozen);

            UpdateResult result = await _usersCollection.UpdateOneAsync(filter, update);

            if (result.MatchedCount == 0)
            {
                bool exists = await _usersCollection.Find(u => u.Id == userId && u.Accounts.Any(a => a.AccountGuid == account.AccountGuid)).AnyAsync();
                
                if (exists)
                {
                    _logger.LogInformation("Discarded stale update for account {AccountGuid}. Newer data already exists.", account.AccountGuid);
                    return; 
                }

                throw new InvalidOperationException($"Account {account.AccountGuid} not found for user {userId}.");
            }

            _logger.LogInformation("Successfully updated account {AccountGuid} for user {UserId}.", account.AccountGuid, userId);
        }
        public async Task UpdateAccountStatusAsync(string userId, string accountGuid, bool isFrozen, string incomingTimestamp)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("Status update failed: UserId is null or empty.");
                throw new InvalidOperationException("UserId cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(accountGuid))
            {
                _logger.LogWarning("Status update failed: AccountGuid is null or empty.");
                throw new InvalidOperationException("AccountGuid cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(incomingTimestamp))
            {
                _logger.LogWarning("Status update failed for account {AccountGuid}: incoming timestamp is missing.", accountGuid);
                throw new InvalidDataException("Incoming timestamp cannot be null or empty.");
            }

            _logger.LogInformation("Updating status for account {AccountGuid} for user {UserId}.", accountGuid, userId);

            FilterDefinition<User> filter = Builders<User>.Filter.And(
                Builders<User>.Filter.Eq(u => u.Id, userId),
                Builders<User>.Filter.ElemMatch(
                    u => u.Accounts,
                    a => a.AccountGuid == accountGuid && 
                    a.Timestamp.CompareTo(incomingTimestamp) < 0
                )
            );

            UpdateDefinition<User> update = Builders<User>.Update.Set("accounts.$.isFrozen", isFrozen);

            UpdateResult result = await _usersCollection.UpdateOneAsync(filter, update);

            if (result.MatchedCount == 0)
            {
                bool accountExists = await _usersCollection.Find(
                    Builders<User>.Filter.And(
                        Builders<User>.Filter.Eq(u => u.Id, userId),
                        Builders<User>.Filter.ElemMatch(u => u.Accounts, a => a.AccountGuid == accountGuid)
                    )
                ).AnyAsync();

                if (accountExists)
                {
                    _logger.LogInformation("Skipped status update for account {AccountGuid}: incoming timestamp {IncomingTimestamp} is not newer than the stored timestamp.", accountGuid, incomingTimestamp);
                    return;
                }

                _logger.LogWarning("Status update failed: Account {AccountGuid} not found for User {UserId}.", accountGuid, userId);
                throw new InvalidOperationException($"No account found for userId '{userId}' and accountGuid '{accountGuid}'.");
            }

            _logger.LogInformation("Successfully updated status for account {AccountGuid} for user {UserId}.", accountGuid, userId);
        }

    }
}
