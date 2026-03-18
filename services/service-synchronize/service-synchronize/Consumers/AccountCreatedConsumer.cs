
using MassTransit;
using service_synchronize.Database;
using service_synchronize.Models;

namespace service_synchronize.Consumers
{
    internal class AccountCreatedConsumer(IAccountsRepository accountRepository) : IConsumer<AccountCreated>
    {
        public async Task Consume(ConsumeContext<AccountCreated> context)
        {
            AccountCreated message = context.Message;
            if (message.Metadata.MessageType != "ACCOUNT_CREATE")
            {
                return;
            }

            Console.WriteLine($"Creating account: {message.Data.Name}");

            Console.WriteLine("Message received.");
            await accountRepository.CreateAccountAsync(message.Data);
        }
    }
}
