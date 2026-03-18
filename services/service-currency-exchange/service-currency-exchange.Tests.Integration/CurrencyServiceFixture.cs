using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using service_currency_exchange.Services;

namespace service_currency_exchange.Tests.Integration;

/// <summary>
/// Fixture that creates a real <see cref="CurrencyService"/> backed by a live
/// <see cref="HttpClient"/> pointing at the Frankfurter public API.
/// No mocking — these tests make actual outbound HTTP calls.
/// </summary>
public sealed class CurrencyServiceFixture : IDisposable
{
    private readonly ServiceProvider _provider;

    public ICurrencyService CurrencyService { get; }

    public CurrencyServiceFixture()
    {
        var services = new ServiceCollection();

        services.AddHttpClient<ICurrencyService, CurrencyService>(client =>
        {
            client.BaseAddress = new Uri("https://api.frankfurter.dev/v1/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        _provider = services.BuildServiceProvider();
        CurrencyService = _provider.GetRequiredService<ICurrencyService>();
    }

    public void Dispose() => _provider.Dispose();
}

