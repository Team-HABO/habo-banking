using MongoDB.Driver;
using service_synchronize.Database;
using service_synchronize.Models;

namespace service_synchronize.tests.IntegrationDatabase
{
    public class CreateTransactionTests : IClassFixture<MongoDbFixture>, IAsyncLifetime
    {
        private readonly MongoDbFixture _fixture;
        private readonly UsersRepository _repository = default!;
        private readonly IMongoCollection<User> _userCollection = default!;
        private static readonly string userId = "user-1";
        private static readonly string accountGuid = "accountGuid";
        private static readonly string validTimestamp = "2026-04-06T09:22:00Z";
        private readonly string invalidAccountGuid = "wrong-accountGuid";
        private readonly string invalidUserId = "wrong-user-id";
        public CreateTransactionTests(MongoDbFixture fixture)
        {
            _fixture = fixture;
            // Use the shared client
            _repository = new UsersRepository(_fixture.Client, "TestDb");
            _userCollection = _fixture.Client.GetDatabase("TestDb").GetCollection<User>("users");
        }
        public Task DisposeAsync() => Task.CompletedTask;

        public async Task InitializeAsync()
        {
            await _userCollection.DeleteManyAsync(FilterDefinition<User>.Empty);
            await SeedUserAsync();
        }
        private async Task SeedUserAsync()
        {
            User user = new()
            {
                Id = userId,
                Accounts =
                [
                    new() {
                        AccountGuid = accountGuid,
                        Balance = new Balance { Amount = 1000.01M },
                        Audits = [
                            new Audit {
                                AuditId = "existing-audit-id",
                                Amount = "100.00",
                                Type = Audit.AuditType.Deposit,
                                Timestamp = validTimestamp
                            }
                        ],
                        Type = Account.AccountType.Main,
                        Name = "My main account",
                        Timestamp = validTimestamp,
                        IsFrozen = false
                    }
                ]
            };
            await _userCollection.InsertOneAsync(user);
        }
        [Fact]
        public async Task UpdateUserWithNewWithdraw_ShouldDecrementBalance_AndPushAudit()
        {
            decimal withdrawAmount = -50.50m;
            Audit audit = new() { AuditId = "msg-1", Amount = "50.50", Type = Audit.AuditType.Withdraw, Timestamp = validTimestamp };

            await _repository.UpdateUserWithNewTransaction(userId, accountGuid, withdrawAmount, audit);

            User? user = await _repository.GetUserByIdAsync(userId);
            Assert.NotNull(user);

            Account account = user.Accounts.First(a => a.AccountGuid == accountGuid);

            Assert.Equal(949.51m, account.Balance.Amount, 2);
            Assert.Equal(949.510m, account.Balance.Amount, 3);
            Assert.Contains(account.Audits, a => a.AuditId == "msg-1");
        }
        [Fact]
        public async Task UpdateUserWithNewDeposit_ShouldIncrementBalance_AndPushAudit()
        {
            decimal depositAmount = 50.50m;
            Audit audit = new() { AuditId = "msg-1", Amount = "50.50", Type = Audit.AuditType.Deposit, Timestamp = validTimestamp };

            await _repository.UpdateUserWithNewTransaction(userId, accountGuid, depositAmount, audit);

            User? user = await _repository.GetUserByIdAsync(userId); 
            Assert.NotNull(user);
            Account account = user.Accounts.First(a => a.AccountGuid == accountGuid);

            Assert.Equal(1050.51m, account.Balance.Amount, 2); 
            Assert.Contains(account.Audits, a => a.AuditId == "msg-1"); 
        }
        [Fact]
        public async Task UpdateUserWithNewTransfer_ShouldDecrementAndIncrement_AndPushTwoAudit()
        {
            User user2 = new()
            {
                Id = "user-2",
                Accounts =
                [
                    new() {
                        AccountGuid = "accountGuid2",
                        Balance = new Balance { Amount = 1000.01M },
                        Audits = [
                            new Audit {
                                AuditId = "existing-audit-id",
                                Amount = "100.00",
                                Type = Audit.AuditType.Deposit,
                                Timestamp = validTimestamp
                            }
                        ],
                        Type = Account.AccountType.Main,
                        Name = "My main account",
                        Timestamp = validTimestamp,
                        IsFrozen = false
                    }
                ]
            };
            await _userCollection.InsertOneAsync(user2);
            decimal amountToTransfer = 50.50m;
            Audit senderAudit = new() { AuditId = "msg-1", Amount = "50.50", Type = Audit.AuditType.Transfer, Timestamp = validTimestamp };
            Audit receiverAudit = new() { AuditId = "msg-1", Amount = "50.50", Type = Audit.AuditType.Transfer, Timestamp = validTimestamp };

            await _repository.ExecuteTransferAsync(userId, accountGuid, amountToTransfer, senderAudit, user2.Id, "accountGuid2", receiverAudit);

            User? user = await _repository.GetUserByIdAsync(userId);
            Assert.NotNull(user);

            Account account = user.Accounts.First(a => a.AccountGuid == accountGuid);

            Assert.Equal(949.51m, account.Balance.Amount, 2);
            Assert.Equal(949.510m, account.Balance.Amount, 3);
            Assert.Contains(account.Audits, a => a.AuditId == "msg-1");

            User? user2FromDatabase = await _repository.GetUserByIdAsync("user-2");
            Assert.NotNull(user2FromDatabase);
            Account account2 = user2FromDatabase.Accounts.First(a => a.AccountGuid == "accountGuid2");

            Assert.Equal(1050.51m, account2.Balance.Amount, 2);
            Assert.Equal(1050.510m, account2.Balance.Amount, 3);
            Assert.Contains(account2.Audits, a => a.AuditId == "msg-1");
        }
        [Fact]
        public async Task UpdateUserWithNewTransaction_ShouldThrow_WhenAccountDoesNotExist()
        {
            Audit audit = new() { Amount = "10.00", AuditId = "msg-2", Timestamp = validTimestamp, Type = Audit.AuditType.Deposit };
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _repository.UpdateUserWithNewTransaction(userId, "non-existent-guid", 10m, audit)
            );
        }

        [Fact]
        public async Task UpdateUserWithNewTransaction_ShouldNotDuplicateAuditOrBalance_WhenAuditIdAlreadyExists()
        {
            Audit duplicateAudit = new()
            {
                AuditId = "existing-audit-id",
                Amount = "10.00",
                Type = Audit.AuditType.Deposit,
                Timestamp = validTimestamp
            };

            await _repository.UpdateUserWithNewTransaction(userId, accountGuid, 10m, duplicateAudit);

            User? user = await _repository.GetUserByIdAsync(userId);
            Assert.NotNull(user);
            Account account = user.Accounts.First(a => a.AccountGuid == accountGuid);

            Assert.Equal(1000.01m, account.Balance.Amount, 2);
            Assert.Single(account.Audits, a => a.AuditId == "existing-audit-id");
        }

        [Fact]
        public async Task ExecuteTransferAsync_ShouldNotDuplicateAuditOrChangeBalances_WhenAuditIdAlreadyExistsOnBothAccounts()
        {
            User user2 = new()
            {
                Id = "user-2",
                Accounts =
                [
                    new() {
                        AccountGuid = "accountGuid2",
                        Balance = new Balance { Amount = 1000.01M },
                        Audits = [
                            new Audit {
                                AuditId = "existing-audit-id",
                                Amount = "100.00",
                                Type = Audit.AuditType.Deposit,
                                Timestamp = validTimestamp
                            }
                        ],
                        Type = Account.AccountType.Main,
                        Name = "My main account",
                        Timestamp = validTimestamp,
                        IsFrozen = false
                    }
                ]
            };
            await _userCollection.InsertOneAsync(user2);

            Audit senderAudit = new() { AuditId = "existing-audit-id", Amount = "50.50", Type = Audit.AuditType.Transfer, Timestamp = validTimestamp };
            Audit receiverAudit = new() { AuditId = "existing-audit-id", Amount = "50.50", Type = Audit.AuditType.Transfer, Timestamp = validTimestamp };

            await _repository.ExecuteTransferAsync(userId, accountGuid, 50.50m, senderAudit, user2.Id, "accountGuid2", receiverAudit);

            User? user1FromDatabase = await _repository.GetUserByIdAsync(userId);
            Assert.NotNull(user1FromDatabase);
            Account account1 = user1FromDatabase.Accounts.First(a => a.AccountGuid == accountGuid);

            User? user2FromDatabase = await _repository.GetUserByIdAsync(user2.Id);
            Assert.NotNull(user2FromDatabase);
            Account account2 = user2FromDatabase.Accounts.First(a => a.AccountGuid == "accountGuid2");

            Assert.Equal(1000.01m, account1.Balance.Amount, 2);
            Assert.Equal(1000.01m, account2.Balance.Amount, 2);
            Assert.Single(account1.Audits, a => a.AuditId == "existing-audit-id");
            Assert.Single(account2.Audits, a => a.AuditId == "existing-audit-id");
        }

        [Fact]
        public async Task AuditExistsAsync_ShouldReturnTrue_WhenAuditExists()
        {
            bool exists = await _repository.AuditExistsAsync(userId, "existing-audit-id");

            Assert.True(exists);
        }
        [Fact]
        public async Task AuditExistsAsync_ShouldReturnFalse_WhenAuditDoesNotExist()
        {
            bool exists = await _repository.AuditExistsAsync(userId, "non-existent-id");

            Assert.False(exists);
        }
        [Fact]
        public async Task AuditExistsAsync_ShouldReturnFalse_WhenUserDoesNotExist()
        {
            bool exists = await _repository.AuditExistsAsync(invalidAccountGuid, "existing-audit-id");

            Assert.False(exists);
        }

        [Fact]
        public async Task GetUserIdByAccountGuidAsync_ShouldReturnUserId_WhenAccountExist()
        {
            string? userIdFromDb = await _repository.GetUserIdByAccountGuidAsync(accountGuid);

            Assert.NotNull(userIdFromDb);
            Assert.Equal(userId, userIdFromDb);
        }

        [Fact]
        public async Task GetUserIdByAccountGuidAsync_ShouldReturnNull_WhenAccountDoesNetExist()
        {
            string? userIdFromDb = await _repository.GetUserIdByAccountGuidAsync(invalidUserId);

            Assert.Null(userIdFromDb);
        }

    }
}
