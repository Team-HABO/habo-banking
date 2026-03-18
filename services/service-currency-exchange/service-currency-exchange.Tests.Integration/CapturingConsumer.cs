using System.Threading.Channels;
using MassTransit;

namespace service_currency_exchange.Tests.Integration;

/// <summary>
/// Generic consumer that captures every received message into an unbounded channel so tests
/// can assert on what <see cref="service_currency_exchange.Consumers.ExchangeRequestedConsumer"/>
/// published to the bus.
/// </summary>
public sealed class CapturingConsumer<T> : IConsumer<T> where T : class
{
    private readonly Channel<T> _channel = Channel.CreateUnbounded<T>();

    public Task Consume(ConsumeContext<T> context)
    {
        _channel.Writer.TryWrite(context.Message);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Drains any buffered messages so state does not bleed between tests.
    /// </summary>
    public void Reset()
    {
        while (_channel.Reader.TryRead(out _)) { }
    }

    /// <summary>
    /// Waits until a message has been captured or the timeout elapses.
    /// Throws <see cref="TimeoutException"/> if no message arrives within the timeout.
    /// </summary>
    public async Task<T> WaitForMessageAsync(TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            return await _channel.Reader.ReadAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException(
                $"No {typeof(T).Name} message was received within {timeout.TotalSeconds}s. " +
                "Make sure the consumer published the message and the host is running.");
        }
    }
}

