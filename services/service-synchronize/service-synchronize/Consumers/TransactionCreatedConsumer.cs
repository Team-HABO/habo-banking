using MassTransit;
using service_synchronize.Messages;
using service_synchronize.Services;

namespace service_synchronize.Consumers
{
    public class TransactionCreatedConsumer(ITransactionService transactionService) : IConsumer<TransactionCreated>
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
                LogContext.Warning?.Log(ex, "Discarding invalid TransactionCreated message with TransactionId {TransactionId}", message.Metadata.MessageId);
                return;
            }
            catch (InvalidOperationException ex)
            {
                LogContext.Warning?.Log(ex, "Discarding malformed TransactionCreated message with TransactionId {TransactionId}", message.Metadata.MessageId);
                return;
            }
        }
    }
}
