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
    ?? "mongodb://localhost:27018";
string rabbitMqHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST")
    ?? "localhost";
HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<IMongoClient>(new MongoClient(mongoConnectionString));
builder.Services.AddScoped<IUsersRepository, UsersRepository>();
builder.Services.AddScoped<IAccountService, AccountService>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<AccountCreatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitMqHost);

        cfg.ReceiveEndpoint("synchronize-account-queue", e =>
        {
            e.UseMessageRetry(r =>
            {
                r.Interval(3, TimeSpan.FromSeconds(5));
                // Do not retry this specific exception
                r.Ignore<InvalidDataException>();
                r.Ignore<MongoWriteException>(ex => ex.WriteError?.Category == ServerErrorCategory.DuplicateKey);
            });
            e.UseRawJsonDeserializer();
            e.ConfigureConsumer<AccountCreatedConsumer>(context);

            e.Bind("synchronize-events", s =>
            {
                s.ExchangeType = "direct"; 
                s.RoutingKey = "account.created"; // Key for direct exchange, might change
            });
        });
    });
});

IHost host = builder.Build();
await host.RunAsync();