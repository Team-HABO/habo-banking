using System.Threading.Channels;
using service_notification.Services;

namespace service_notification.Tests.Integration;

/// <summary>
///     Decorator that forwards every call to the real <see cref="IEmailService" /> and
///     captures each invocation so tests can assert on the arguments without polling an
///     external inbox.
/// </summary>
public sealed class CapturingEmailService(IEmailService inner) : IEmailService
{
    private readonly Channel<EmailCall> _channel = Channel.CreateUnbounded<EmailCall>();

    public async Task<string> SendEmailAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        // Capture the invocation before calling the inner service so the test can observe
        // the call even if the real EmailService throws (e.g. SMTP throttling).
        await _channel.Writer.WriteAsync(new EmailCall(toEmail, toName, subject, htmlBody));
        return await inner.SendEmailAsync(toEmail, toName, subject, htmlBody);
    }

    /// <summary>
    ///     Drains any previously captured calls so each test starts with a clean slate.
    /// </summary>
    public void Reset()
    {
        while (_channel.Reader.TryRead(out _))
        {
        }
    }

    /// <summary>
    ///     Waits until an email has been captured or the timeout elapses.
    ///     Throws <see cref="TimeoutException" /> if no call arrives within the timeout.
    /// </summary>
    public async Task<EmailCall> WaitForCallAsync(TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            return await _channel.Reader.ReadAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException(
                $"No email was sent within {timeout.TotalSeconds}s. " +
                "Make sure the consumer received the message and the host is running.");
        }
    }

    public record EmailCall(string ToEmail, string ToName, string Subject, string HtmlBody);
}