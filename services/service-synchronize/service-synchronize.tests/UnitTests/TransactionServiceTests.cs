using Moq;
using Microsoft.Extensions.Logging;
using service_synchronize.Services;
using service_synchronize.Database;
using service_synchronize.Messages;
using service_synchronize.Models;

namespace service_synchronize.tests.UnitTests
{
    public class TransactionServiceTests
    {
        private readonly Mock<IUsersRepository> _repoMock;
        private readonly Mock<ILogger<TransactionService>> _loggerMock;
        private readonly TransactionService _service;

        public TransactionServiceTests()
        {
            _repoMock = new Mock<IUsersRepository>();
            _loggerMock = new Mock<ILogger<TransactionService>>();
            _service = new TransactionService(_repoMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task ProcessTransaction_ShouldReturn_WhenAuditAlreadyExists()
        {
            TransactionCreated message = CreateMessage("DEPOSIT", "100.00");
            _repoMock.Setup(r => r.AuditExistsAsync(It.IsAny<string>(), It.IsAny<string>()))
                     .ReturnsAsync(true);

            await _service.ProcessTransaction(message);

            _repoMock.Verify(r => r.UpdateUserWithNewTransaction(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<Audit>()), Times.Never);
        }
        [Theory]
        [InlineData("not-a-number")] // Case: Parsing fails
        [InlineData("-50.00")]       // Case: Amount is negative
        [InlineData("0")]            // Case: Amount is zero
        public async Task ProcessTransaction_ShouldThrowInvalidDataException_WhenAmountIsInvalid(string invalidAmount)
        {
            TransactionCreated message = CreateMessage("DEPOSIT", invalidAmount);

            InvalidDataException exception = await Assert.ThrowsAsync<InvalidDataException>(() =>
                _service.ProcessTransaction(message)
            );

            Assert.Contains(invalidAmount, exception.Message);
        }

        [Fact]
        public async Task ProcessTransaction_ShouldUpdateBalance_OnDeposit()
        {
            TransactionCreated message = CreateMessage("DEPOSIT", "50.00");
            _repoMock.Setup(r => r.AuditExistsAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);

            await _service.ProcessTransaction(message);

            _repoMock.Verify(r => r.UpdateUserWithNewTransaction(
                "user1",
                "acc1",
                50.00m, 
                It.IsAny<Audit>()),
                Times.Once);
        }
        [Fact]
        public async Task ProcessTransaction_ShouldUpdateBalance_OnWithdraw()
        {
            TransactionCreated message = CreateMessage("WITHDRAW", "50.00");
            _repoMock.Setup(r => r.AuditExistsAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);

            await _service.ProcessTransaction(message);

            _repoMock.Verify(r => r.UpdateUserWithNewTransaction(
                "user1",
                "acc1",
                -50.00m,
                It.IsAny<Audit>()),
                Times.Once);
        }

        [Fact]
        public async Task ProcessTransaction_ShouldLogWarning_OnInvalidType()
        {
            TransactionCreated message = CreateMessage("INVALID_TYPE", "100.00");

            await _service.ProcessTransaction(message);

            _loggerMock.Verify(l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("invalid Type")), 
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), 
                Times.Once);
        }

        [Fact]
        public async Task ProcessTransaction_ShouldExecuteTransfer_WithMappedAudits()
        {
            TransactionCreated message = CreateTransferMessage("75.00", "sender-account-name", "receiver-account-name");
            _repoMock.Setup(r => r.AuditExistsAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);
            _repoMock.Setup(r => r.GetUserIdByAccountGuidAsync("acc2")).ReturnsAsync("user2");
            Audit? capturedSenderAudit = null;
            Audit? capturedReceiverAudit = null;

            _repoMock
                .Setup(r => r.ExecuteTransferAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<decimal>(),
                    It.IsAny<Audit>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Audit>()))
                .Callback<string, string, decimal, Audit, string, string, Audit>((_, _, _, senderAudit, _, _, receiverAudit) =>
                {
                    capturedSenderAudit = senderAudit;
                    capturedReceiverAudit = receiverAudit;
                });

            await _service.ProcessTransaction(message);

            _repoMock.Verify(r => r.ExecuteTransferAsync(
                It.Is<string>(s => s == "user1"),
                It.Is<string>(s => s == "acc1"),
                It.Is<decimal>(d => d == 75.00m),
                It.IsAny<Audit>(),
                It.Is<string>(s => s == "user2"),
                It.Is<string>(s => s == "acc2"),
                It.IsAny<Audit>()),
                Times.Once);

            Assert.NotNull(capturedSenderAudit);
            Assert.NotNull(capturedReceiverAudit);
            Assert.Equal("receiver-account-name", capturedSenderAudit!.ReceiverAccountName);
            Assert.Equal("sender-account-name", capturedReceiverAudit!.SenderAccountName);
        }

        [Fact]
        public async Task ProcessTransaction_ShouldThrowInvalidOperationException_WhenTransferReceiverIsMissing()
        {
            TransactionCreated message = CreateTransferMessage("75.00", "sender-account-name", "receiver-account-name");
            message.Data.Receiver = null;
            _repoMock.Setup(r => r.AuditExistsAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.ProcessTransaction(message)
            );

            Assert.Contains("missing receiver data", exception.Message);
        }

        [Fact]
        public async Task ProcessTransaction_ShouldThrowInvalidDataException_WhenReceiverUserDoesNotExist()
        {
            TransactionCreated message = CreateTransferMessage("75.00", "sender-account-name", "receiver-account-name");
            _repoMock.Setup(r => r.AuditExistsAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);
            _repoMock.Setup(r => r.GetUserIdByAccountGuidAsync("acc2")).ReturnsAsync((string?)null);

            InvalidDataException exception = await Assert.ThrowsAsync<InvalidDataException>(() =>
                _service.ProcessTransaction(message)
            );

            Assert.Contains("acc2", exception.Message);
            _repoMock.Verify(r => r.GetUserIdByAccountGuidAsync("acc2"), Times.Once);
            _repoMock.Verify(r => r.ExecuteTransferAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<decimal>(),
                It.IsAny<Audit>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Audit>()),
                Times.Never);
        }

        private static TransactionCreated CreateMessage(string type, string amount) => new()
        {
            Metadata = new() { MessageType = type, MessageId = "msg123", MessageTimestamp = "2026-04-06T09:21:00Z" },
            Data = new()
            {
                OwnerId = "user1",
                Account = new()
                {
                    Guid = "acc1",
                    Audit = new() { Amount = amount, Type = "DEPOSIT", Timestamp = "now" }
                }
            }
        };

        private static TransactionCreated CreateTransferMessage(string amount, string senderName, string receiverName) => new()
        {
            Metadata = new() { MessageType = "TRANSACTION_TRANSFER", MessageId = "msg123", MessageTimestamp = "2026-04-06T09:21:00Z" },
            Data = new()
            {
                OwnerId = "user1",
                Account = new()
                {
                    Guid = "acc1",
                    Audit = new() { Amount = amount, Type = "TRANSFER", Timestamp = "now", ReceiverAccountName = receiverName }
                },
                Receiver = new()
                {
                    Guid = "acc2",
                    Audit = new() { Amount = amount, Type = "TRANSFER", Timestamp = "now", ReceiverAccountName = senderName }
                }
            }
        };
    }
}
