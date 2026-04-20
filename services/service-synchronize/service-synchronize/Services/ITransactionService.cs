using service_synchronize.Messages;

namespace service_synchronize.Services
{
    public interface ITransactionService
    {
        Task ProcessTransaction(TransactionCreated message);
    }
}