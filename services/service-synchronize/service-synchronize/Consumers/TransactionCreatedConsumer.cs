using MassTransit;
using Microsoft.Extensions.Logging;
using service_synchronize.Messages;
using service_synchronize.Services;

namespace service_synchronize.Consumers
{
    public class TransactionCreatedConsumer(ITransactionService transactionService, ILogger<TransactionCreatedConsumer> logger) : IConsumer<TransactionCreated>
    {
        public async Task Consume(ConsumeContext<TransactionCreated> context)
        {
            TransactionCreated message = context.Message;

            try
            {
                await transactionService.ProcessTransaction(message);
            }
            catch (InvalidDataException ex)
            {
                logger.LogWarning(ex, "Discarding invalid TransactionCreated message with TransactionId {TransactionId}", message.Metadata.MessageId);
                return;
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Discarding malformed TransactionCreated message with TransactionId {TransactionId}", message.Metadata.MessageId);
                return;
            }
        }
    }
}
