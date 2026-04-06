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
            _repositoryMock.Setup(repo => repo.GetUserByIdAsync(userId)).ReturnsAsync((User?)null);

            await _service.ProcessAccountCreationAsync(userId, firstAccountDto);
            _repositoryMock.Verify(r =>
                r.CreateUserWithAccountAsync(It.Is<User>(u =>
                    u.Id == userId &&
                    u.Accounts.Count == 1 &&
                    u.Accounts[0].AccountGuid == firstAccountDto.AccountGuid
                )),
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
            _repositoryMock .Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(existingUser);

            await _service.ProcessAccountCreationAsync(userId, secondAccountDto);

            _repositoryMock.Verify(r => r.AddAccountToUserAsync(
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
            _repositoryMock.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(existingUser);
            await _service.ProcessAccountCreationAsync(userId, firstAccountDto);


            _repositoryMock.Verify(r => r.AddAccountToUserAsync(It.IsAny<string>(), It.IsAny<Account>()), Times.Never);
            _repositoryMock.Verify(r => r.CreateUserWithAccountAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task ProcessAccountCreationAsync_ShouldThrow_WhenUserIdIsNullOrEmpty()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.ProcessAccountCreationAsync("", firstAccountDto));
        }
    }
}
