using MassTransit;
using Microsoft.Extensions.Options;
using service_notification.Messages;
using service_notification.Services;
using service_notification.Settings;

namespace service_notification.Consumers;

public class NotificationConsumer(
    ILogger<NotificationConsumer> logger,
    IEmailService emailService,
    IOptions<EmailSettings> emailOptions)
    : IConsumer<Notification>
{
    private readonly EmailSettings _emailSettings = emailOptions.Value;

    public async Task Consume(ConsumeContext<Notification> context)
    {
        var notification = context.Message;

        logger.LogInformation(
            "Received notification of type {MessageType} at {Timestamp}: {Message}",
            notification.Metadata.MessageType,
            notification.Metadata.MessageTimestamp,
            notification.Data.Message);

        var subject = notification.Metadata.MessageType switch
        {
            "FraudNotification"    => "HABO Bank - Potential fraud notification!",
            "ExchangeNotification" => "HABO Bank - Currency exchange failed!",
            _                      => "HABO Bank - Notification"
        };

        var sanitizedMessage = System.Net.WebUtility.HtmlEncode(notification.Data.Message);

        await emailService.SendEmailAsync(
            _emailSettings.FromEmail,
            _emailSettings.FromName,
            subject,
            $"<h1>{System.Net.WebUtility.HtmlEncode(subject)}</h1><p>{sanitizedMessage}</p>"
        );
    }
}