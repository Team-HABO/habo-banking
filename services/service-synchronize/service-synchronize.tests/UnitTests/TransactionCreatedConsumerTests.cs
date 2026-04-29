using MassTransit;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using service_synchronize.Consumers;
using service_synchronize.Messages;
using service_synchronize.Services;

namespace service_synchronize.tests.UnitTests
{
    public class TransactionCreatedConsumerTests
    {
        private readonly Mock<ITransactionService> _serviceMock;
        private readonly TransactionCreatedConsumer _consumer;
        private readonly Mock<ConsumeContext<TransactionCreated>> _contextMock;

        public TransactionCreatedConsumerTests()
        {
            _serviceMock = new Mock<ITransactionService>();
            _consumer = new TransactionCreatedConsumer(_serviceMock.Object, NullLogger<TransactionCreatedConsumer>.Instance);
            _contextMock = new Mock<ConsumeContext<TransactionCreated>>();
        }

        [Fact]
        public async Task Consume_HappyPath_ShouldCallService()
        {
            TransactionCreated message = CreateValidMessage();
            _contextMock.Setup(c => c.Message).Returns(message);
            _serviceMock.Setup(s => s.ProcessTransaction(message))
                        .Returns(Task.CompletedTask);

            await _consumer.Consume(_contextMock.Object);

            _serviceMock.Verify(s => s.ProcessTransaction(message), Times.Once);
        }

        [Fact]
        public async Task Consume_WhenServiceThrowsInvalidDataException_ShouldDiscardAndNotThrow()
        {
            TransactionCreated message = CreateValidMessage();
            _contextMock.Setup(c => c.Message).Returns(message);

            _serviceMock.Setup(s => s.ProcessTransaction(It.IsAny<TransactionCreated>()))
                        .ThrowsAsync(new InvalidDataException("Simulated validation error"));

            Exception exception = await Record.ExceptionAsync(() => _consumer.Consume(_contextMock.Object));

            Assert.Null(exception); 
            _serviceMock.Verify(s => s.ProcessTransaction(message), Times.Once);
        }

        [Fact]
        public async Task Consume_WhenServiceThrowsInvalidOperationException_ShouldDiscardAndNotThrow()
        {
            TransactionCreated message = CreateValidMessage();
            _contextMock.Setup(c => c.Message).Returns(message);

            _serviceMock.Setup(s => s.ProcessTransaction(It.IsAny<TransactionCreated>()))
                        .ThrowsAsync(new InvalidOperationException("Missing receiver data"));

            Exception exception = await Record.ExceptionAsync(() => _consumer.Consume(_contextMock.Object));

            Assert.Null(exception);
        }

        [Fact]
        public async Task Consume_WhenInfrastructureFails_ShouldLetExceptionBubbleUp()
        {
            TransactionCreated message = CreateValidMessage();
            _contextMock.Setup(c => c.Message).Returns(message);

            _serviceMock.Setup(s => s.ProcessTransaction(It.IsAny<TransactionCreated>()))
                        .ThrowsAsync(new Exception("Database connection failed"));

            await Assert.ThrowsAsync<Exception>(() => _consumer.Consume(_contextMock.Object));
        }

        private static TransactionCreated CreateValidMessage() => new()
        {
            Metadata = new TransactionMetadata { MessageId = "123", MessageType = "DEPOSIT", MessageTimestamp = "2026-04-06T09:21:00Z" },
            Data = new TransactionCreatedData
            {
                OwnerId = "user1",
                Account = new TransactionCreatedAccountDto
                {
                    Guid = "acc1",
                    Audit = new AuditDto { Amount = "100", Type = "DEPOSIT", Timestamp = "now" }
                }
            }
        };
    }
}
