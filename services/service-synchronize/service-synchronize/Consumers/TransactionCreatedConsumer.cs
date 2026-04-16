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

            await transactionService.ProcessTransaction(message);
        }
    }
}
