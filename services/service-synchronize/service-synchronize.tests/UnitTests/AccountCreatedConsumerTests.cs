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

        private readonly Metadata md = new() { MessageTimestamp = DateTime.Now, MessageType = "ACCOUNT_CREATE" };
        [Fact]
        public async Task Consumer_Should_Process_Message_When_Published()
        {
            await using ServiceProvider provider = new ServiceCollection()
                .AddMassTransitTestHarness(x =>
                {
                    x.AddConsumer<AccountCreatedConsumer>();
                })
                .AddScoped(_ => new Mock<IAccountService>().Object) 
                .BuildServiceProvider();

            ITestHarness harness = provider.GetRequiredService<ITestHarness>();
            await harness.Start();
            Data messageData = new() { Account = firstAccount, OwnerId = "user-1" };

            await harness.Bus.Publish(new AccountCreated
            {
                Data = messageData,
                Metadata = md
            });

            Assert.True(await harness.Consumed.Any<AccountCreated>());

            IConsumerTestHarness<AccountCreatedConsumer> consumerHarness = harness.GetConsumerHarness<AccountCreatedConsumer>();
            Assert.True(await consumerHarness.Consumed.Any<AccountCreated>());
        }
    }
}
