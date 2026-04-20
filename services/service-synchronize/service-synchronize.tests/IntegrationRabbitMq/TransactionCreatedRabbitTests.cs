using System.Text;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RabbitMQ.Client;
using service_synchronize.Consumers;
using service_synchronize.Messages;
using service_synchronize.Services;
using Testcontainers.RabbitMq;

namespace service_synchronize.tests.IntegrationRabbitMq
{
    public class TransactionCreatedRabbitTests : IAsyncLifetime
    {
        private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder("rabbitmq:3-management").Build();
        private IServiceProvider _serviceProvider = default!;
        private  IBusControl _bus = default!;
        private readonly Mock<ITransactionService> _serviceMock = new();
       
        public async Task DisposeAsync()
        {
            await _bus.StopAsync();
            await _rabbitMqContainer.DisposeAsync();
        }
        private readonly string rawJson = @"
    {
        ""data"": {
            ""ownerId"": ""user-1"",
            ""account"": {
                ""guid"": ""acc-abc"",
                ""audit"": {
                    ""amount"": ""150.50"",
                    ""type"": ""DEPOSIT"",
                    ""timestamp"": ""2026-04-16T07:49:52Z""
                }
            }
        },
        ""metadata"": {
            ""messageId"": ""999"",
            ""messageType"": ""DEPOSIT"",
            ""messageTimestamp"": ""2026-04-16T07:49:52Z""
        }
    }";
        public async Task InitializeAsync()
        {
            await _rabbitMqContainer.StartAsync();

            ServiceCollection services = new();

            services.AddSingleton(_serviceMock.Object);
            services.AddLogging();

            services.AddMassTransit(x =>
            {
                x.AddConsumer<TransactionCreatedConsumer>();

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(_rabbitMqContainer.GetConnectionString());
                    cfg.ClearSerialization();
                    cfg.UseRawJsonSerializer(RawSerializerOptions.AnyMessageType, true);

                    cfg.ReceiveEndpoint("synchronize-transaction-queue", e =>
                    {
                        e.ConfigureConsumer<TransactionCreatedConsumer>(context);

                        e.Bind("synchronize-events", s =>
                        {
                            s.ExchangeType = "direct";
                            s.RoutingKey = "synchronize-transaction";
                        });
                    });
                });
            });

            _serviceProvider = services.BuildServiceProvider();
            _bus = _serviceProvider.GetRequiredService<IBusControl>();
            await _bus.StartAsync();
        }

        [Fact]
        public async Task SendMessage_ShouldTriggerConsumer_AndCallService()
        {
            TaskCompletionSource<bool> signal = new();
            _serviceMock
                .Setup(s => s.ProcessTransaction(It.IsAny<TransactionCreated>()))
                .Returns(Task.CompletedTask)
                .Callback(() => signal.SetResult(true));

            ConnectionFactory factory = new() { Uri = new Uri(_rabbitMqContainer.GetConnectionString()) };
            using IConnection connection = await factory.CreateConnectionAsync();
            using IChannel channel = await connection.CreateChannelAsync();

            await channel.ExchangeDeclareAsync("synchronize-events", "direct", durable: true);
            await channel.QueueDeclareAsync("synchronize-transaction-queue", durable: true, exclusive: false, autoDelete: false);
            await channel.QueueBindAsync("synchronize-transaction-queue", "synchronize-events", "synchronize-transaction");


            BasicProperties properties = new() { ContentType = "application/json" };
            byte[] body = Encoding.UTF8.GetBytes(rawJson);

            await channel.BasicPublishAsync(
                exchange: "synchronize-events",
                routingKey: "synchronize-transaction",
                mandatory: false,
                basicProperties: properties,
                body: body);

            Task completedTask = await Task.WhenAny(signal.Task, Task.Delay(TimeSpan.FromSeconds(5)));
            Assert.True(completedTask == signal.Task, "MassTransit failed to deserialize the raw JSON bytes.");
        }
    }
}
