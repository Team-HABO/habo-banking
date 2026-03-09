using FluentAssertions;
using service_ai.Models;
using service_ai.Services;
using Xunit;

namespace service_ai.Tests.Integration;

[Trait("Category", "Integration")]
public class OpenRouterServiceTests(OpenRouterServiceFixture fixture) : IClassFixture<OpenRouterServiceFixture>
{
    private readonly IOpenRouterService _sut = fixture.OpenRouterService;

    // Builds the same prompt format that CheckFraudConsumer sends (Amount + Transaction Type + Origin IP)
    private static string BuildPrompt(
        decimal amount,
        string transactionType,
        string originIpAddress) =>
        $"Amount: {amount}\n" +
        $"Transaction Type: {transactionType}\n" +
        $"Origin IP Address: {originIpAddress}";

    // -------------------------------------------------------------------------
    // Shape tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SendPromptAsync_When_GivenAValidTransaction_Should_ReturnValidShape()
    {
        var prompt = BuildPrompt(500, "transfer", "82.211.100.1");

        var result = await _sut.SendPromptAsync(prompt);

        result.Should().NotBeNull();
        result.Should().BeOfType<FraudCheckResult>();
        result.Reason.Should().NotBeNullOrEmpty();
        result.RiskScore.Should().BeInRange(0.0, 1.0);
    }

    // -------------------------------------------------------------------------
    // Heuristic: clear transaction (low amount, safe IP)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SendPromptAsync_When_AmountIsBelowThreshold_Should_ReturnFraudFalse()
    {
        // Amount well below 10 000, no high-risk country IP
        var prompt = BuildPrompt(250, "transfer", "82.211.100.1");

        var result = await _sut.SendPromptAsync(prompt);

        result.IsFraud.Should().BeFalse(because: $"amount is well below the 10 000 threshold. Reason: {result.Reason}");
    }

    // -------------------------------------------------------------------------
    // Heuristic: high-value transaction (amount > 10 000)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SendPromptAsync_When_AmountExceedsThreshold_Should_ReturnFraudTrue()
    {
        // Amount clearly exceeds the 10 000 threshold
        var prompt = BuildPrompt(15_000, "transfer", "82.211.100.1");

        var result = await _sut.SendPromptAsync(prompt);

        result.IsFraud.Should().BeTrue(because: $"amount of 15 000 exceeds the 10 000 threshold. Reason: {result.Reason}");
    }
}



