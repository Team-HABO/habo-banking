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
        private readonly AccountCreatedAccountDto firstAccount = TestData.CreateAccountDto("1");
        private readonly string userId = "user-1";
        

        private readonly AccountCreatedMetadata md = new() { MessageTimestamp = "2026-04-06T09:22:00Z", MessageType = "ACCOUNT_CREATE" };
        [Fact]
        public async Task Consume_ValidMessageType_ShouldCallService()
        {
            Mock<IAccountService> accountServiceMock = new();
            await using ServiceProvider provider = CreateProvider(accountServiceMock.Object);
            ITestHarness harness = provider.GetRequiredService<ITestHarness>();
            await harness.Start();
            AccountCreatedData messageData = new() { Account = firstAccount, OwnerId = userId };

            await harness.Bus.Publish(new AccountCreated
            {
                Data = messageData,
                Metadata = md
            });

            Assert.True(await harness.Consumed.Any<AccountCreated>());
            accountServiceMock.Verify(
                s => s.ProcessAccountCreationAsync(userId, It.IsAny<AccountCreatedAccountDto>()),
                Times.Once
            );
        }
        [Fact]
        public async Task Consume_InvalidMessageType_ShouldDiscardAndNotCallService()
        {
            Mock<IAccountService> accountServiceMock = new();

            await using ServiceProvider provider = CreateProvider(accountServiceMock.Object);
            ITestHarness harness = provider.GetRequiredService<ITestHarness>();
            await harness.Start();

            await harness.Bus.Publish(new AccountCreated
            {
                Data = new() { Account = firstAccount, OwnerId = userId },
                Metadata = new() { MessageType = "invalid_type", MessageTimestamp = "2026-04-06T09:22:00Z" }
            });

            Assert.True(await harness.GetConsumerHarness<AccountCreatedConsumer>().Consumed.Any<AccountCreated>());

            accountServiceMock.Verify(s => s.ProcessAccountCreationAsync(
                It.IsAny<string>(),
                It.IsAny<AccountCreatedAccountDto>()),
                Times.Never);
        }
        [Fact]
        public async Task Consume_InvalidAccountType_ShouldDiscardAndNotCallService()
        {
            Mock<IAccountService> accountServiceMock = new();

            await using ServiceProvider provider = CreateProvider(accountServiceMock.Object);
            ITestHarness harness = provider.GetRequiredService<ITestHarness>();
            await harness.Start();

            AccountCreated invalidAccountDto = new()
            {
                Data = new() { Account = firstAccount, OwnerId = userId },
                Metadata = md
            };
            invalidAccountDto.Data.Account.Type = "wrongType";
            
            await harness.Bus.Publish(invalidAccountDto);

            Assert.True(await harness.GetConsumerHarness<AccountCreatedConsumer>().Consumed.Any<AccountCreated>());

            accountServiceMock.Verify(s => s.ProcessAccountCreationAsync(
                It.IsAny<string>(),
                It.IsAny<AccountCreatedAccountDto>()),
                Times.Never);
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
