using MassTransit;
using MongoDB.Driver;
using service_synchronize.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using service_synchronize.Consumers;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<IMongoClient>(new MongoClient("mongodb://localhost:27018"));
builder.Services.AddScoped<IUsersRepository, UsersRepository>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<AccountCreatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost");

        cfg.ReceiveEndpoint("AccountCreated", e =>
        {
            e.ConfigureConsumer<AccountCreatedConsumer>(context);
        });
    });
});

IHost host = builder.Build();
await host.RunAsync();