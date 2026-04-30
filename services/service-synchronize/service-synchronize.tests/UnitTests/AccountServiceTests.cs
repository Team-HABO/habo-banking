using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using service_synchronize.Database;
using service_synchronize.Messages;
using service_synchronize.Models;
using service_synchronize.Services;

namespace service_synchronize.tests.UnitTests
{
   public class AccountServiceTests
   {
      private readonly Mock<IUsersRepository> _repositoryMock = new();
      private readonly AccountService _service;
      private readonly string _userId = "user-1";
        private static readonly string MessageTimestamp = "2026-04-06T09:22:00Z";


        public AccountServiceTests()
      {
         _service = new AccountService(_repositoryMock.Object, NullLogger<AccountService>.Instance);
      }

      [Fact]
      public async Task ProcessAccountCreationAsync_ShouldMapAccountAndCallUpsertAccountAsync()
      {
         AccountDetail accountDto = TestData.CreateAccountDto("acc-1", "Savings Account");

         await _service.ProcessAccountCreationAsync(_userId, accountDto);

         _repositoryMock.Verify(r => r.UpsertAccountAsync(
            _userId,
            It.Is<Account>(account =>
               account.AccountGuid == accountDto.AccountGuid &&
               account.Name == accountDto.Name &&
               account.IsFrozen == accountDto.IsFrozen &&
               account.Timestamp == accountDto.Timestamp &&
               account.Type == Account.AccountType.Savings &&
               account.Balance.Amount == 0M &&
               account.Audits.Count == 0)),
            Times.Once);
      }

      [Fact]
      public async Task ProcessAccountCreationAsync_ShouldThrow_WhenAccountIsNull()
      {
         await Assert.ThrowsAsync<InvalidDataException>(() =>
            _service.ProcessAccountCreationAsync(_userId, null!));

         _repositoryMock.Verify(r => r.UpsertAccountAsync(It.IsAny<string>(), It.IsAny<Account>()), Times.Never);
      }

      [Fact]
      public async Task ProcessAccountCreationAsync_ShouldThrow_WhenUserIdIsMissing()
      {
         AccountDetail accountDto = TestData.CreateAccountDto("acc-1");

         await Assert.ThrowsAsync<InvalidDataException>(() =>
            _service.ProcessAccountCreationAsync(string.Empty, accountDto));

         _repositoryMock.Verify(r => r.UpsertAccountAsync(It.IsAny<string>(), It.IsAny<Account>()), Times.Never);
      }

      [Fact]
      public async Task ProcessAccountUpdateAsync_ShouldMapAccountAndCallUpdateAccountAsync()
      {
         AccountDetail accountDto = TestData.CreateAccountDto("acc-2", "Updated Account");
         accountDto.Type = "Pension";
         accountDto.IsFrozen = true;

         await _service.ProcessAccountUpdateAsync(_userId, accountDto);

         _repositoryMock.Verify(r => r.UpdateAccountAsync(
            _userId,
            It.Is<Account>(account =>
               account.AccountGuid == accountDto.AccountGuid &&
               account.Name == accountDto.Name &&
               account.Timestamp == accountDto.Timestamp &&
                  account.Type == Account.AccountType.Pension &&
                  account.IsFrozen &&
               account.Balance.Amount == 0M)),
            Times.Once);
      }

      [Fact]
      public async Task ProcessAccountUpdateAsync_ShouldThrow_WhenAccountIsNull()
      {
         await Assert.ThrowsAsync<InvalidDataException>(() =>
            _service.ProcessAccountUpdateAsync(_userId, null!));

         _repositoryMock.Verify(r => r.UpdateAccountAsync(It.IsAny<string>(), It.IsAny<Account>()), Times.Never);
      }

      [Fact]
      public async Task ProcessAccountUpdateAsync_ShouldThrow_WhenAccountTypeIsInvalid()
      {
         AccountDetail accountDto = TestData.CreateAccountDto("acc-2", "Updated Account");
         accountDto.Type = "NotARealType";

         await Assert.ThrowsAsync<InvalidDataException>(() =>
            _service.ProcessAccountUpdateAsync(_userId, accountDto));

         _repositoryMock.Verify(r => r.UpdateAccountAsync(It.IsAny<string>(), It.IsAny<Account>()), Times.Never);
      }

      [Fact]
      public async Task ProcessAccountUpdateAsync_ShouldThrow_WhenUserIdIsMissing()
      {
         AccountDetail accountDto = TestData.CreateAccountDto("acc-2", "Updated Account");

         await Assert.ThrowsAsync<InvalidDataException>(() =>
            _service.ProcessAccountUpdateAsync(string.Empty, accountDto));

         _repositoryMock.Verify(r => r.UpdateAccountAsync(It.IsAny<string>(), It.IsAny<Account>()), Times.Never);
      }

      [Fact]
      public async Task ProcessStatusChangeAsync_ShouldThrow_WhenIncomingTimestampIsMissing()
      {
         await Assert.ThrowsAsync<InvalidDataException>(() =>
            _service.ProcessStatusChangeAsync(_userId, "acc-3", true, string.Empty));

         _repositoryMock.Verify(r => r.UpdateAccountStatusAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()), Times.Never);
      }

      [Fact]
      public async Task ProcessStatusChangeAsync_ShouldThrow_WhenUserIdIsMissing()
      {
         await Assert.ThrowsAsync<InvalidDataException>(() =>
            _service.ProcessStatusChangeAsync(string.Empty, "acc-3", true, MessageTimestamp));

         _repositoryMock.Verify(r => r.UpdateAccountStatusAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()), Times.Never);
      }

      [Fact]
      public async Task ProcessStatusChangeAsync_ShouldThrow_WhenAccountGuidIsMissing()
      {
         await Assert.ThrowsAsync<InvalidDataException>(() =>
            _service.ProcessStatusChangeAsync(_userId, string.Empty, true, MessageTimestamp));

         _repositoryMock.Verify(r => r.UpdateAccountStatusAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()), Times.Never);
      }

      [Fact]
      public async Task ProcessStatusChangeAsync_ShouldCallUpdateAccountStatusAsync()
      {
         await _service.ProcessStatusChangeAsync(_userId, "acc-3", true, MessageTimestamp);

         _repositoryMock.Verify(r => r.UpdateAccountStatusAsync(_userId, "acc-3", true, MessageTimestamp), Times.Once);
      }

      [Fact]
      public async Task ProcessAccountDeletionAsync_ShouldCallDeleteAccountAsync()
      {
         await _service.ProcessAccountDeletionAsync(_userId, "acc-4");

         _repositoryMock.Verify(r => r.DeleteAccountAsync(_userId, "acc-4"), Times.Once);
      }
   }
}
