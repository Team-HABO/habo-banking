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
        private readonly AccountDto firstAccount = TestData.CreateAccountDto("1");

        private readonly AccountCreatedMetadata md = new() { MessageTimestamp = DateTime.Now, MessageType = "ACCOUNT_CREATE" };
        [Fact]
        public async Task Consumer_Should_Process_Message_When_Published()
        {
            Mock<IAccountService> accountServiceMock = new();
            string userId = "user-1";
            await using ServiceProvider provider = new ServiceCollection()
                .AddMassTransitTestHarness(x =>
                {
                    x.AddConsumer<AccountCreatedConsumer>();
                })
                .AddSingleton(accountServiceMock.Object)
                .AddLogging()
                .BuildServiceProvider();

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
                s => s.ProcessAccountCreationAsync(userId, It.IsAny<AccountDto>()),
                Times.Once
            );
        }
    }
}
