using Moq;
using service_synchronize.Database;
using service_synchronize.Messages;
using service_synchronize.Models;
using service_synchronize.Services;

namespace service_synchronize.tests.UnitTests
{
    public class AccountServiceTests
    {
        private readonly Mock<IUsersRepository> _repositoryMock;
        private readonly AccountService _service;
        private readonly string userId = "111";
        private readonly AccountDto firstAccountDto = TestData.CreateAccountDto("1");
        private readonly AccountDto secondAccountDto = TestData.CreateAccountDto("2");
        public AccountServiceTests()
        {
            _repositoryMock = new Mock<IUsersRepository>();
            _service = new AccountService(_repositoryMock.Object);
        }

        [Fact]
        public async Task ProcessAccountCreationAsync_ShouldCallUpsertAccountAsync_WhenUserDoesNotExist()
        {

            await _service.ProcessAccountCreationAsync(userId, firstAccountDto);

            _repositoryMock.Verify(r =>
                r.UpsertAccountAsync(
                    userId,
                    It.Is<Account>(a => a.AccountGuid == firstAccountDto.AccountGuid)
                ),
                Times.Once);
        }

        [Fact]
        public async Task ProcessAccountCreationAsync_ShouldCallUpsertAccountAsync_WhenUserExistsAndAccountDoesNotExist()
        {
            Account firstAccount = AccountService.MapAccountToModel(firstAccountDto);
            Account secondAccount = AccountService.MapAccountToModel(secondAccountDto);

            await _service.ProcessAccountCreationAsync(userId, secondAccountDto);

            _repositoryMock.Verify(r => r.UpsertAccountAsync(
                userId,
                It.Is<Account>(a => a.AccountGuid == secondAccount.AccountGuid)
            ), Times.Once);
        }

        [Fact]
        public async Task ProcessAccountCreationAsync_ShouldCallUpsertAccountAsync_WhenAccountAlreadyExists()
        {
            Account firstAccount = AccountService.MapAccountToModel(firstAccountDto);

            await _service.ProcessAccountCreationAsync(userId, firstAccountDto);

            _repositoryMock.Verify(r =>
                r.UpsertAccountAsync(userId, It.IsAny<Account>()),
                Times.Once);
        }
        [Fact]
        public async Task ProcessAccountCreationAsync_ShouldThrow_WhenDtoIsNull()
        {
            await Assert.ThrowsAsync<InvalidDataException>(() =>
                _service.ProcessAccountCreationAsync(userId, null!));
        }

        [Fact]
        public async Task ProcessAccountCreationAsync_ShouldThrow_WhenUserIdIsNullOrEmpty()
        {
            await Assert.ThrowsAsync<InvalidDataException>(() =>
                _service.ProcessAccountCreationAsync("", firstAccountDto));
            await Assert.ThrowsAsync<InvalidDataException>(() =>
                _service.ProcessAccountCreationAsync(null!, firstAccountDto));
        }
        [Fact]
        public async Task ProcessAccountCreationAsync_ShouldMapAllFieldsCorrectly_AndCallRepository()
        {
            string timestamp = DateTime.UtcNow.ToString();
            BalanceDto bDto = new()
            { 
                Amount = "100", 
                Timestamp = timestamp 
            };
            AccountDto dto = new()
            {
                AccountGuid = "guid-123",
                Name = "My savings",
                Type = "SAVINGS",
                Balance = bDto,
                IsFrozen = false,
                Timestamp = timestamp
            };

            await _service.ProcessAccountCreationAsync(userId, dto);

            _repositoryMock.Verify(r => r.UpsertAccountAsync(
                userId,
                It.Is<Account>(a =>
                    a.AccountGuid == dto.AccountGuid &&
                    a.Type == Account.AccountType.Savings &&
                    a.Name == dto.Name &&
                    a.IsFrozen == dto.IsFrozen &&
                    a.Timestamp == dto.Timestamp &&
                    a.Balances.Count == 1
                )
            ), Times.Once);
        }
        [Fact]
        public async Task ProcessAccountCreationAsync_ShouldThrow_AndCallRepository()
        {
            string timestamp = DateTime.UtcNow.ToString();
            BalanceDto? bDto = null;
            AccountDto dto = new()
            {
                AccountGuid = "guid-123",
                Name = "My savings",
                Type = "SAVINGS",
                Balance = bDto,
                IsFrozen = false,
                Timestamp = timestamp
            };

            await Assert.ThrowsAsync<InvalidDataException>(async () => await _service.ProcessAccountCreationAsync(userId, dto));
        }
    }
}
