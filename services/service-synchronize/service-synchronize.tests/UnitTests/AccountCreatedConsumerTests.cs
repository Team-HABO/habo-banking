using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using service_synchronize.Consumers;
using service_synchronize.Messages;
using service_synchronize.Services;

namespace service_synchronize.tests.UnitTests
{
    public class AccountCreatedConsumerTests
    {
        private readonly AccountDetail firstAccount = TestData.CreateAccountDto("1");
        private readonly AccountDetail secondAccount = TestData.CreateAccountDto("2");
        private readonly string userId = "user-1";

        private static readonly string MessageTimestamp = "2026-04-06T09:22:00Z";

        [Fact]
        public async Task Consume_AccountCreate_ShouldCallCreateMethod()
        {
            Mock<IAccountService> accountServiceMock = new();
            await using ServiceProvider provider = CreateProvider(accountServiceMock.Object);
            ITestHarness harness = provider.GetRequiredService<ITestHarness>();
            await harness.Start();

            await harness.Bus.Publish(CreateMessage("ACCOUNT_CREATE", firstAccount));

            Assert.True(await harness.Consumed.Any<AccountEventEnvelope>());
            accountServiceMock.Verify(
                s => s.ProcessAccountCreationAsync(userId, It.IsAny<AccountDetail>()),
                Times.Once
            );
            accountServiceMock.Verify(s => s.ProcessAccountUpdateAsync(It.IsAny<string>(), It.IsAny<AccountDetail>()), Times.Never);
            accountServiceMock.Verify(s => s.ProcessStatusChangeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
            accountServiceMock.Verify(s => s.ProcessAccountDeletionAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Consume_AccountDelete_ShouldCallDeleteMethod()
        {
            Mock<IAccountService> accountServiceMock = new();

            await using ServiceProvider provider = CreateProvider(accountServiceMock.Object);
            ITestHarness harness = provider.GetRequiredService<ITestHarness>();
            await harness.Start();

            await harness.Bus.Publish(CreateMessage("ACCOUNT_DELETE", secondAccount));

            Assert.True(await harness.GetConsumerHarness<AccountCreatedConsumer>().Consumed.Any<AccountEventEnvelope>());
            accountServiceMock.Verify(s => s.ProcessAccountDeletionAsync(userId, "2"), Times.Once);
            accountServiceMock.Verify(s => s.ProcessAccountCreationAsync(It.IsAny<string>(), It.IsAny<AccountDetail>()), Times.Never);
            accountServiceMock.Verify(s => s.ProcessAccountUpdateAsync(It.IsAny<string>(), It.IsAny<AccountDetail>()), Times.Never);
            accountServiceMock.Verify(s => s.ProcessStatusChangeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task Consume_AccountStatus_ShouldCallStatusMethod()
        {
            Mock<IAccountService> accountServiceMock = new();

            await using ServiceProvider provider = CreateProvider(accountServiceMock.Object);
            ITestHarness harness = provider.GetRequiredService<ITestHarness>();
            await harness.Start();

            AccountDetail frozenAccount = TestData.CreateAccountDto("2");
            frozenAccount.IsFrozen = true;

            await harness.Bus.Publish(CreateMessage("ACCOUNT_STATUS", frozenAccount));

            Assert.True(await harness.GetConsumerHarness<AccountCreatedConsumer>().Consumed.Any<AccountEventEnvelope>());
            accountServiceMock.Verify(s => s.ProcessStatusChangeAsync(userId, "2", true), Times.Once);
            accountServiceMock.Verify(s => s.ProcessAccountCreationAsync(It.IsAny<string>(), It.IsAny<AccountDetail>()), Times.Never);
            accountServiceMock.Verify(s => s.ProcessAccountUpdateAsync(It.IsAny<string>(), It.IsAny<AccountDetail>()), Times.Never);
            accountServiceMock.Verify(s => s.ProcessAccountDeletionAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Consume_AccountUpdate_ShouldCallUpdateMethod()
        {
            Mock<IAccountService> accountServiceMock = new();

            await using ServiceProvider provider = CreateProvider(accountServiceMock.Object);
            ITestHarness harness = provider.GetRequiredService<ITestHarness>();
            await harness.Start();

            await harness.Bus.Publish(CreateMessage("ACCOUNT_UPDATE", secondAccount));

            Assert.True(await harness.GetConsumerHarness<AccountCreatedConsumer>().Consumed.Any<AccountEventEnvelope>());
            accountServiceMock.Verify(s => s.ProcessAccountUpdateAsync(userId, It.IsAny<AccountDetail>()), Times.Once);
            accountServiceMock.Verify(s => s.ProcessAccountCreationAsync(It.IsAny<string>(), It.IsAny<AccountDetail>()), Times.Never);
            accountServiceMock.Verify(s => s.ProcessStatusChangeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
            accountServiceMock.Verify(s => s.ProcessAccountDeletionAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Consume_InvalidMessageType_ShouldDiscardAndNotCallAnyServiceMethod()
        {
            Mock<IAccountService> accountServiceMock = new();

            await using ServiceProvider provider = CreateProvider(accountServiceMock.Object);
            ITestHarness harness = provider.GetRequiredService<ITestHarness>();
            await harness.Start();

            await harness.Bus.Publish(CreateMessage("invalid_type", firstAccount));

            Assert.True(await harness.GetConsumerHarness<AccountCreatedConsumer>().Consumed.Any<AccountEventEnvelope>());

            accountServiceMock.Verify(s => s.ProcessAccountCreationAsync(
                It.IsAny<string>(),
                It.IsAny<AccountDetail>()),
                Times.Never);
            accountServiceMock.Verify(s => s.ProcessAccountUpdateAsync(It.IsAny<string>(), It.IsAny<AccountDetail>()), Times.Never);
            accountServiceMock.Verify(s => s.ProcessStatusChangeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
            accountServiceMock.Verify(s => s.ProcessAccountDeletionAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        private static AccountEventEnvelope CreateMessage(string messageType, AccountDetail account)
        {
            return new AccountEventEnvelope
            {
                Data = new AccountEventData
                {
                    OwnerId = "user-1",
                    Account = account
                },
                Metadata = new AccountMetadata
                {
                    MessageType = messageType,
                    MessageTimestamp = MessageTimestamp
                }
            };
        }

        //Helper to create the service provider for the tests
        private static ServiceProvider CreateProvider(IAccountService service)
        {
            return new ServiceCollection()
                .AddMassTransitTestHarness(x => x.AddConsumer<AccountCreatedConsumer>())
                .AddSingleton(service)
                .AddLogging()
                .BuildServiceProvider();
        }
    }
}
