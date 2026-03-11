using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using service_ai.Services;

namespace service_ai.Tests.Integration;

public class OpenRouterServiceFixture : IAsyncLifetime
{
    public IOpenRouterService OpenRouterService { get; private set; } = null!;

    public Task InitializeAsync()
    {
        // Walk up from the test runner working directory to find the .env at the service root
        DotNetEnv.Env.TraversePath().Load();

        var apiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ArgumentException("OPENROUTER_API_KEY is not set. Skipping OpenRouter integration tests. Configure this variable (e.g., via .env) to enable these tests.");
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OpenRouter:ApiKey"] = apiKey,
                ["OpenRouter:Model"] = "stepfun/step-3.5-flash"
            })
            .Build();

        var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
        var logger = loggerFactory.CreateLogger<OpenRouterService>();

        var httpClient = new HttpClient();
        OpenRouterService = new OpenRouterService(httpClient, logger, configuration);

        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;
}


