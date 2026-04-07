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
        public async Task ProcessAccountCreationAsync_ShouldCallCreateUserWithAccountAsync_WhenUserDoesNotExist()
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
        public async Task ProcessAccountCreationAsync_ShouldAddAccount_WhenUserExistsAndAccountDoesNotExist()
        {
            Account firstAccount = AccountService.MapAccountToModel(firstAccountDto);
            Account secondAccount = AccountService.MapAccountToModel(secondAccountDto);
            User existingUser = new()
            {
                Id = userId,
                Accounts = [firstAccount]
            };

            await _service.ProcessAccountCreationAsync(userId, secondAccountDto);

            _repositoryMock.Verify(r => r.UpsertAccountAsync(
                userId,
                It.Is<Account>(a => a.AccountGuid == secondAccount.AccountGuid)
            ), Times.Once);
        }

        [Fact]
        public async Task ProcessAccountCreationAsync_ShouldDoNothing_WhenAccountAlreadyExists()
        {
            Account firstAccount = AccountService.MapAccountToModel(firstAccountDto);

            User existingUser = new()
            {
                Id = userId,
                Accounts = [firstAccount]
            };
            await _service.ProcessAccountCreationAsync(userId, firstAccountDto);

            _repositoryMock.Verify(r =>
                r.UpsertAccountAsync(userId, It.IsAny<Account>()),
                Times.Once);
        }

        [Fact]
        public async Task ProcessAccountCreationAsync_ShouldThrow_WhenUserIdIsNullOrEmpty()
        {
            await Assert.ThrowsAsync<InvalidDataException>(() =>
                _service.ProcessAccountCreationAsync("", firstAccountDto));
        }
    }
}
