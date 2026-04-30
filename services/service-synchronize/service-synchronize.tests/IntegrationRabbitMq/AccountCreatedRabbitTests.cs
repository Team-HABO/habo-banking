using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using service_synchronize.Consumers;
using service_synchronize.Messages;
using service_synchronize.Services;
using Testcontainers.RabbitMq;

namespace service_synchronize.tests.IntegrationRabbitMq
{
    public class AccountCreatedRabbitTests : IAsyncLifetime
    {
        private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder("rabbitmq:3-management").Build();
        private IServiceProvider _serviceProvider = default!;
        private  IBusControl _bus = default!;
        private readonly Mock<IAccountService> _serviceMock = new();
        private readonly AccountDetail firstAccount = TestData.CreateAccountDto("1");
        private readonly AccountDetail secondAccount = TestData.CreateAccountDto("2");

        private readonly AccountMetadata md = new() { MessageTimestamp = "2026-04-06T09:22:00Z", MessageType = "ACCOUNT_CREATE" };
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
                x.AddConsumer<AccountEventConsumer>();

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(_rabbitMqContainer.GetConnectionString());

                    cfg.ReceiveEndpoint("synchronize-account-queue-test", e =>
                    {
                        e.UseRawJsonDeserializer(); 
                        e.ConfigureConsumer<AccountEventConsumer>(context);

                        e.Bind("synchronize-events", s =>
                        {
                            s.ExchangeType = "direct";
                            s.RoutingKey = "synchronize-account-queue";
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
        public async Task SendAccountCreateMessage_ShouldTriggerCreateHandler()
        {
            TaskCompletionSource<bool> signal = new();

            _serviceMock
                .Setup(s => s.ProcessAccountCreationAsync("user-1", It.IsAny<AccountDetail>()))
                .Returns(Task.CompletedTask)
                .Callback(() => signal.SetResult(true));

            AccountEventData messageData = new() { Account = firstAccount, OwnerId = "user-1" };
            AccountEventEnvelope message = new() { Data = messageData, Metadata = md };

            await _bus.Publish(message);

            Task completedTask = await Task.WhenAny(signal.Task, Task.Delay(10000));

            Assert.True(completedTask == signal.Task, "The consumer was not triggered within the timeout period.");

            _serviceMock.Verify(s => s.ProcessAccountCreationAsync("user-1", It.IsAny<AccountDetail>()), Times.Once);
        }

        [Fact]
        public async Task SendAccountUpdateMessage_ShouldTriggerUpdateHandler()
        {
            TaskCompletionSource<bool> signal = new();

            _serviceMock
                .Setup(s => s.ProcessAccountUpdateAsync("user-1", It.IsAny<AccountDetail>()))
                .Returns(Task.CompletedTask)
                .Callback(() => signal.SetResult(true));

            AccountEventEnvelope message = new()
            {
                Data = new() { Account = secondAccount, OwnerId = "user-1" },
                Metadata = new() { MessageTimestamp = "2026-04-06T09:22:00Z", MessageType = "ACCOUNT_UPDATE" }
            };

            await _bus.Publish(message);

            Task completedTask = await Task.WhenAny(signal.Task, Task.Delay(10000));

            Assert.True(completedTask == signal.Task, "The consumer was not triggered within the timeout period.");

            _serviceMock.Verify(s => s.ProcessAccountUpdateAsync("user-1", It.IsAny<AccountDetail>()), Times.Once);
        }

        [Fact]
        public async Task SendAccountStatusMessage_ShouldTriggerStatusHandler()
        {
            TaskCompletionSource<bool> signal = new();

            _serviceMock
                .Setup(s => s.ProcessStatusChangeAsync("user-1", "2", true, md.MessageTimestamp))
                .Returns(Task.CompletedTask)
                .Callback(() => signal.SetResult(true));

            AccountDetail statusAccount = TestData.CreateAccountDto("2");
            statusAccount.IsFrozen = true;

            AccountEventEnvelope message = new()
            {
                Data = new() { Account = statusAccount, OwnerId = "user-1" },
                Metadata = new() { MessageTimestamp = "2026-04-06T09:22:00Z", MessageType = "ACCOUNT_STATUS" }
            };

            await _bus.Publish(message);

            Task completedTask = await Task.WhenAny(signal.Task, Task.Delay(10000));

            Assert.True(completedTask == signal.Task, "The consumer was not triggered within the timeout period.");

            _serviceMock.Verify(s => s.ProcessStatusChangeAsync("user-1", "2", true, md.MessageTimestamp), Times.Once);
        }

        [Fact]
        public async Task SendAccountDeleteMessage_ShouldTriggerDeleteHandler()
        {
            TaskCompletionSource<bool> signal = new();

            _serviceMock
                .Setup(s => s.ProcessAccountDeletionAsync("user-1", "2"))
                .Returns(Task.CompletedTask)
                .Callback(() => signal.SetResult(true));

            AccountEventEnvelope message = new()
            {
                Data = new() { Account = secondAccount, OwnerId = "user-1" },
                Metadata = new() { MessageTimestamp = "2026-04-06T09:22:00Z", MessageType = "ACCOUNT_DELETE" }
            };

            await _bus.Publish(message);

            Task completedTask = await Task.WhenAny(signal.Task, Task.Delay(10000));

            Assert.True(completedTask == signal.Task, "The consumer was not triggered within the timeout period.");

            _serviceMock.Verify(s => s.ProcessAccountDeletionAsync("user-1", "2"), Times.Once);
        }
    }
}
