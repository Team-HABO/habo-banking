using FluentAssertions;
using service_ai.Models;
using service_ai.Services;
using Xunit;

namespace service_ai.Tests.Integration;

[Trait("Category", "Integration")]
public class OpenRouterServiceTests(OpenRouterServiceFixture fixture) : IClassFixture<OpenRouterServiceFixture>
{
    private readonly IOpenRouterService _sut = fixture.OpenRouterService;

    // Builds the same prompt format that CheckFraudConsumer sends
    private static string BuildPrompt(
        string senderAccount,
        string receiverAccount,
        decimal amount,
        string currency,
        string originIpAddress) =>
        $"Sender Account: {senderAccount}\n" +
        $"Receiver Account: {receiverAccount}\n" +
        $"Amount: {amount}\n" +
        $"Currency: {currency}\n" +
        $"Origin IP Address: {originIpAddress}";

    // -------------------------------------------------------------------------
    // Shape tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SendPromptAsync_When_GivenAValidTransaction_Should_ReturnValidShape()
    {
        var prompt = BuildPrompt("DK0000000001", "DK0000000002", 500, "DKK", "82.211.100.1");

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
        var prompt = BuildPrompt("DK0000000001", "DK0000000002", 250, "DKK", "82.211.100.1");

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
        var prompt = BuildPrompt("DK0000000001", "DK0000000002", 15_000, "DKK", "82.211.100.1");

        var result = await _sut.SendPromptAsync(prompt);

        result.IsFraud.Should().BeTrue(because: $"amount of 15 000 exceeds the 10 000 threshold. Reason: {result.Reason}");
    }
}



