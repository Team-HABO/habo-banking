using MassTransit;
using Microsoft.Extensions.Options;
using service_notification.Messages;
using service_notification.Services;
using service_notification.Settings;

namespace service_notification.Consumers;

public class FraudNotificationConsumer(
    ILogger<FraudNotificationConsumer> logger,
    IEmailService emailService,
    IOptions<EmailSettings> emailOptions)
    : IConsumer<FraudNotification>
{
    private readonly EmailSettings _emailSettings = emailOptions.Value;

    public async Task Consume(ConsumeContext<FraudNotification> context)
    {
        var notification = context.Message;

        logger.LogInformation(
            "Received fraud notification at {Timestamp}: {Message}",
            notification.Metadata.MessageTimestamp,
            notification.Data.Message);

        var sanitizedMessage = System.Net.WebUtility.HtmlEncode(notification.Data.Message);

        await emailService.SendEmailAsync(
            _emailSettings.FromEmail,
            _emailSettings.FromName,
            "HABO Bank - Potential fraud notification!",
            $"<h1>Potential fraud detected!</h1><p>{sanitizedMessage}</p>"
        );
    }
}