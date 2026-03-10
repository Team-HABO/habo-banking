using FluentAssertions;
using service_notification.Services;

namespace service_notification.Tests.Integration;

[Trait("Category", "Integration")]
public class EmailServiceTests(EmailServiceFixture fixture) : IClassFixture<EmailServiceFixture>
{
    private readonly IEmailService _sut = fixture.EmailService;

    [Fact]
    public async Task SendEmailAsync_WhenCalledWithValidCredentials_ShouldReturnMailtrapQueuedResponse()
    {
        var response = await _sut.SendEmailAsync(
            fixture.ToEmail,
            fixture.ToName,
            "HABO Bank - Integration Test",
            "<h1>Integration test</h1><p>This is a test email sent from the integration test suite.</p>"
        );

        response.Should().StartWith("2.0.0").And.Contain("queued");
    }
}

