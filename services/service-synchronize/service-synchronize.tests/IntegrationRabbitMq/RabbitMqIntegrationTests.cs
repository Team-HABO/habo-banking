using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using service_synchronize.Consumers;
using service_synchronize.Messages;
using service_synchronize.Services;
using Testcontainers.RabbitMq;

namespace service_synchronize.tests.IntegrationRabbitMq
{
    public class RabbitMqIntegrationTests : IAsyncLifetime
    {
        private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder("rabbitmq:3-management").Build();
        private IServiceProvider _serviceProvider = default!;
        private  IBusControl _bus = default!;
        private readonly Mock<IAccountService> _serviceMock = new();
        private readonly AccountDto firstAccount = TestData.CreateAccountDto("1");

        private readonly AccountCreatedMetadata md = new() { MessageTimestamp = DateTime.Now, MessageType = "ACCOUNT_CREATE" };
        public async Task DisposeAsync()
        {
            await _bus.StopAsync();
            await _rabbitMqContainer.DisposeAsync();
        }

        public async Task InitializeAsync()
        {
            await _rabbitMqContainer.StartAsync();

            ServiceCollection services = new();
            services.AddLogging();

            services.AddMassTransit(x =>
            {
                x.AddConsumer<AccountCreatedConsumer>();

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(_rabbitMqContainer.GetConnectionString());

                    cfg.ReceiveEndpoint("synchronize-account-queue-test", e =>
                    {
                        e.UseRawJsonDeserializer(); 
                        e.ConfigureConsumer<AccountCreatedConsumer>(context);

                        e.Bind("synchronize-events", s =>
                        {
                            s.ExchangeType = "direct";
                            s.RoutingKey = "account.created";
                        });
                    });
                });
            });

            services.AddSingleton(_serviceMock.Object);
            _serviceProvider = services.BuildServiceProvider();
            _bus = _serviceProvider.GetRequiredService<IBusControl>();

            await _bus.StartAsync();
        }

        [Fact]
        public async Task SendMessage_ShouldTriggerConsumer_AndCallService()
        {
            // Creates a signal for when method in _serviceMock is called
            TaskCompletionSource<bool> signal = new();

            _serviceMock
                .Setup(s => s.ProcessAccountCreationAsync("user-1", It.IsAny<AccountDto>()))
                .Returns(Task.CompletedTask)
                .Callback(() => signal.SetResult(true)); // Signal success!

            AccountCreatedData messageData = new() { Account = firstAccount, OwnerId = "user-1" };
            AccountCreated message = new() { Data = messageData, Metadata = md };

            await _bus.Publish(message);

            // Wait for signal or timeout (10 seconds)
            Task completedTask = await Task.WhenAny(signal.Task, Task.Delay(10000));

            // Assert that signal.Task completed first. If not it prints error message
            Assert.True(completedTask == signal.Task, "The consumer was not triggered within the timeout period.");

            _serviceMock.Verify(s => s.ProcessAccountCreationAsync("user-1", It.IsAny<AccountDto>()), Times.Once);
        }
    }
}
