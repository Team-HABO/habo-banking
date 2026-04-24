using MassTransit;
using MongoDB.Driver;
using DotNetEnv;
using service_synchronize.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using service_synchronize.Consumers;
using service_synchronize.Services;

// TraversePath() helps locate .env in parent directories
// NoClobber makes sure a .env file does not override env variables in docker compose
Env.NoClobber().TraversePath().Load();


string mongoConnectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING")
    ?? "mongodb://localhost:27017";
string rabbitMqHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST")
    ?? "localhost";
HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<IMongoClient>(new MongoClient(mongoConnectionString));
builder.Services.AddScoped<IUsersRepository, UsersRepository>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();

builder.Services.AddMassTransit(x => {
    x.AddConsumer<AccountCreatedConsumer>();
    x.AddConsumer<TransactionCreatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitMqHost);

        cfg.ClearSerialization(); 
        cfg.UseRawJsonSerializer(RawSerializerOptions.AnyMessageType, true);

        cfg.ReceiveEndpoint("synchronize-account-queue", e =>
        {
            e.UseMessageRetry(r => {
                r.Interval(3, TimeSpan.FromSeconds(5));
                r.Ignore<InvalidDataException>();
            });
            
            e.ConfigureConsumer<AccountCreatedConsumer>(context);

            e.Bind("synchronize-events", s => {
                s.ExchangeType = "direct"; 
                s.RoutingKey = "synchronize-account";
            });
        });

        cfg.ReceiveEndpoint("synchronize-transaction-queue", e =>
        {
            e.UseMessageRetry(r => {
                r.Interval(3, TimeSpan.FromSeconds(5));
                r.Ignore<InvalidDataException>();
            });

            e.ConfigureConsumer<TransactionCreatedConsumer>(context);

            e.Bind("synchronize-events", s => {
                s.ExchangeType = "direct";
                s.RoutingKey = "synchronize-transaction-queue";
            });
        });
    });
});

IHost host = builder.Build();
await host.RunAsync();